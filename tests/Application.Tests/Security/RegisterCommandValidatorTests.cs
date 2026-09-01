using Application.Security.Register;
using Xunit;

namespace Application.Tests.Security;

public sealed class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator validator = new();

    public static TheoryData<RegisterCommand, string> InvalidCommands => new()
    {
        { Valid() with { IdentificationNumber = string.Empty }, nameof(RegisterCommand.IdentificationNumber) },
        { Valid() with { IdentificationNumber = "   " }, nameof(RegisterCommand.IdentificationNumber) },
        { Valid() with { IdentificationNumber = new string('9', 21) }, nameof(RegisterCommand.IdentificationNumber) }
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Validate_rejects_invalid_command_property(
        RegisterCommand command,
        string propertyName)
    {
        var result = await validator.ValidateAsync(command);

        Assert.Contains(result.Errors, failure => failure.PropertyName == propertyName);
    }

    [Fact]
    public async Task Validate_accepts_a_complete_command_with_a_20_character_identification_number()
    {
        var result = await validator.ValidateAsync(Valid() with { IdentificationNumber = new string('9', 20) });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_accepts_complete_command()
    {
        var result = await validator.ValidateAsync(Valid());

        Assert.True(result.IsValid);
    }

    private static RegisterCommand Valid() => new(
        FullName: "Ana Cliente",
        Email: "ana.cliente@huellitas.test",
        UserName: "ana.cliente",
        Password: "Password123!",
        IdentificationNumber: "1234567890");
}
