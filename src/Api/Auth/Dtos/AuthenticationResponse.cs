using Application.Security.Models;

namespace Api.Auth.Dtos;
public sealed record AuthenticationResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt)
{
    public static AuthenticationResponse From(AuthenticationTokens tokens) =>
        new(
            tokens.AccessToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresAt);
}