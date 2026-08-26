namespace Infrastructure.Security.Tokens;
public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAt);