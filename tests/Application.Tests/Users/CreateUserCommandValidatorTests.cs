using FluentValidation.TestHelper;
using Application.Users.UseCase;
using Xunit;

namespace Application.Tests.Users;

// T10-a: crear usuario siempre exige contraseña (>= 8).
public sealed class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator sut = new();

    [Fact]
    public void Validate_fails_when_password_is_missing()
    {
        var result = sut.TestValidate(Valid() with { Password = "" });

        result.ShouldHaveValidationErrorFor(command => command.Password);
    }

    [Fact]
    public void Validate_fails_when_password_is_shorter_than_8()
    {
        var result = sut.TestValidate(Valid() with { Password = "short" });

        result.ShouldHaveValidationErrorFor(command => command.Password);
    }

    [Fact]
    public void Validate_succeeds_when_password_has_at_least_8_characters()
    {
        var result = sut.TestValidate(Valid());

        result.ShouldNotHaveValidationErrorFor(command => command.Password);
    }

    private static CreateUserCommand Valid() => new(
        "Ana Staff",
        "ana@huellitas.test",
        "Password123!",
        Guid.Parse("11111111-1111-1111-1111-111111111111"));
}
