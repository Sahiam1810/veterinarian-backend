using Application.UserTokens.UseCase;
using Xunit;

namespace Application.Tests.UserTokens;

// P0 corregido: este validator es la primera barrera contra forjar un
// refresh token para cualquier cuenta vía POST /api/usertokens (ver
// CreateUserTokenCommandHandler, que antes persistía el TokenValue tal cual
// sin restringir el TokenType).
public sealed class CreateUserTokenCommandValidatorTests
{
    private readonly CreateUserTokenCommandValidator validator = new();

    [Theory]
    [InlineData("refresh")]
    [InlineData("REFRESH")]
    [InlineData("Refresh")]
    [InlineData(" refresh ")]
    [InlineData("access")]
    [InlineData("ACCESS")]
    public async Task Validate_rejects_reserved_token_types(string tokenType)
    {
        var result = await validator.ValidateAsync(Valid() with { TokenType = tokenType });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(CreateUserTokenCommand.TokenType));
    }

    [Theory]
    [InlineData("reset_password")]
    [InlineData("email_verification")]
    public async Task Validate_accepts_non_authenticating_token_types(string tokenType)
    {
        var result = await validator.ValidateAsync(Valid() with { TokenType = tokenType });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_rejects_empty_token_type_without_throwing()
    {
        var result = await validator.ValidateAsync(Valid() with { TokenType = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(CreateUserTokenCommand.TokenType));
    }

    private static CreateUserTokenCommand Valid() => new(
        AccountId: Guid.NewGuid(),
        TokenValue: "some-hash-value",
        TokenType: "reset_password",
        ExpiresAt: DateTime.UtcNow.AddHours(1));
}
