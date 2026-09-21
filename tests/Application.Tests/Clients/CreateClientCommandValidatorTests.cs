using Application.Clients.Errors;
using Application.Clients.UseCases;
using Domain.Clients.ValueObjects;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.Tests.Clients;

public sealed class CreateClientCommandValidatorTests
{
    private readonly CreateClientCommandValidator validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_rejects_missing_or_blank_phone(string? phone)
    {
        var result = validator.TestValidate(Valid() with { PhoneNumber = phone });

        result.ShouldHaveValidationErrorFor(command => command.PhoneNumber)
            .WithErrorCode(ClientErrorCodes.PhoneRequired);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12ab34")]
    [InlineData("123456")]
    [InlineData("123456789012345678901")]
    public void Validate_rejects_invalid_phone_format(string phone)
    {
        var result = validator.TestValidate(Valid() with { PhoneNumber = phone });

        result.ShouldHaveValidationErrorFor(command => command.PhoneNumber)
            .WithErrorCode(ClientErrorCodes.PhoneInvalidFormat);
    }

    [Fact]
    public void Validate_accepts_a_valid_phone_that_the_value_object_can_normalize()
    {
        const string rawPhone = "+57 (300) 123-4567";
        var command = Valid() with { PhoneNumber = rawPhone };

        var result = validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.PhoneNumber);
        Assert.Equal("573001234567", ClientPhoneNumber.Create(command.PhoneNumber!).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_rejects_a_blank_full_name(string name)
    {
        var result = validator.TestValidate(Valid() with { FullName = name });

        result.ShouldHaveValidationErrorFor(c => c.FullName);
    }

    [Fact]
    public void Validate_rejects_a_full_name_longer_than_the_maximum()
    {
        var result = validator.TestValidate(Valid() with { FullName = new string('a', 151) });

        result.ShouldHaveValidationErrorFor(c => c.FullName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-es-un-correo")]
    [InlineData("sin@dominio")]
    public void Validate_rejects_a_missing_or_malformed_email(string email)
    {
        var result = validator.TestValidate(Valid() with { Email = email });

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validate_rejects_an_email_longer_than_the_maximum()
    {
        var result = validator.TestValidate(Valid() with { Email = new string('a', 146) + "@x.co" });

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validate_accepts_a_valid_name_and_email()
    {
        var result = validator.TestValidate(Valid());

        result.ShouldNotHaveValidationErrorFor(c => c.FullName);
        result.ShouldNotHaveValidationErrorFor(c => c.Email);
    }

    private static CreateClientCommand Valid() => new(
        FullName: "Ana Cliente",
        Email: "ana@huellitas.test",
        IdentificationNumber: "1234567890",
        PhoneNumber: "3001234567",
        Address: "Calle Falsa 123");
}
