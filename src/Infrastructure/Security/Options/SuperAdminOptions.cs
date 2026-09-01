namespace Infrastructure.Security.Options;

public sealed class SuperAdminOptions
{
    public const string SectionName = "SuperAdmin";

    public bool Enabled { get; init; }

    public Guid Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public string PasswordHash { get; init; } = string.Empty;
}
