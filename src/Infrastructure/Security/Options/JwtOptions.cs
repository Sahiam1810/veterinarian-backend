namespace Infrastructure.Security.Options;
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string PrivateKeyPemBase64 { get; init; } = string.Empty;

    public string PublicKeyPemBase64 { get; init; } = string.Empty;

    public string KeyId { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public int AccessTokenMinutes { get; init; }

    public int RefreshTokenDays { get; init; }

    public int ClockSkewSeconds { get; init; }
}
