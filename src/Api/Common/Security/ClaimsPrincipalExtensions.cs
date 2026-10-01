using System.Security.Claims;
using Domain.Roles;

namespace Api.Common.Security;

public static class ClaimsPrincipalExtensions
{
    public static bool IsSuperAdmin(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirst("role_id")?.Value, out var roleId) &&
        SystemRoles.IsSuperAdmin(roleId);

    /// <summary>
    /// Lee el id del cliente del token delegado del bot: la claim <c>sub</c> (o
    /// <see cref="ClaimTypes.NameIdentifier"/>) es el id del cliente. Rechaza
    /// ausente, no-GUID y <see cref="Guid.Empty"/>.
    /// </summary>
    public static bool TryGetClientId(this ClaimsPrincipal principal, out Guid clientId)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        return Guid.TryParse(subject, out clientId) && clientId != Guid.Empty;
    }
}
