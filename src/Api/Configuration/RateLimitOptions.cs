public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public int GlobalPermitLimit { get; init; }
    public int GlobalWindowSeconds { get; init; }
    public int LoginPermitLimit { get; init; }
    public int LoginWindowSeconds { get; init; }
    public int RefreshPermitLimit { get; init; }
    public int RefreshWindowSeconds { get; init; }
    public int RegisterPermitLimit { get; init; }
    public int RegisterWindowSeconds { get; init; }
}