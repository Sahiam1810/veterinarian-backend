using Application.Clients.Errors;
using Application.Clients.UseCases;
using Domain.Clients.ValueObjects;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.Tests.Clients;

public sealed class UpdateClientCommandValidatorTests
{
    private readonly UpdateClientCommandValidator validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_rejects_emptying_the_phone(string? phone)
    {
        var result = validator.TestValidate(Valid() with { PhoneNumber = phone });

        result.ShouldHaveValidationErrorFor(command => command.PhoneNumber)
            .WithErrorCode(ClientErrorCodes.PhoneRequired);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123456")]
    public void Validate_rejects_invalid_phone_format(string phone)
    {
        var result = validator.TestValidate(Valid() with { PhoneNumber = phone });

        result.ShouldHaveValidationErrorFor(command => command.PhoneNumber)
            .WithErrorCode(ClientErrorCodes.PhoneInvalidFormat);
    }

    [Fact]
    public void Validate_accepts_a_valid_phone()
    {
        const string rawPhone = "+57 301 555 1234";
        var command = Valid() with { PhoneNumber = rawPhone };

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
        Assert.Equal("573015551234", ClientPhoneNumber.Create(command.PhoneNumber!).Value);
    }

    private static UpdateClientCommand Valid() => new(
        Id: Guid.NewGuid(),
        UserId: Guid.NewGuid(),
        IdentificationNumber: "1234567890",
        Address: "Calle Falsa 123",
        PhoneNumber: "3001234567");
}
