namespace Api.Auth.Dtos;

public sealed record RevokeTokenRequest(
    string RefreshToken);