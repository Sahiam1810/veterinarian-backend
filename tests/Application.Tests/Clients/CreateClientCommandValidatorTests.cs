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

    private static CreateClientCommand Valid() => new(
        UserId: Guid.NewGuid(),
        IdentificationNumber: "1234567890",
        Address: "Calle Falsa 123",
        PhoneNumber: "3001234567");
}
