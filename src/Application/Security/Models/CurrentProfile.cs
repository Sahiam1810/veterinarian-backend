namespace Application.Security.Models;

public sealed record CurrentProfile(
    Guid Id,
    string FullName,
    string Initials,
    string Email,
    string Role,
    string? PhotoUrl = null)
{
    public static CurrentProfile From(AuthenticatedIdentity identity)
    {
        var fullName = identity.FullName.Trim();
        var parts = fullName.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);

        var initials = parts.Length switch
        {
            0 => string.Empty,
            1 => char.ToUpperInvariant(parts[0][0]).ToString(),
            _ => $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}"
        };

        return new CurrentProfile(
            identity.UserId,
            fullName,
            initials,
            identity.Email,
            identity.Role,
            identity.PhotoUrl);
    }
}
