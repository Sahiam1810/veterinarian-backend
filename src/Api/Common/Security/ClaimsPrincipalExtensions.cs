using System.Security.Claims;
using Domain.Roles;

namespace Api.Common.Security;

public static class ClaimsPrincipalExtensions
{
    public static bool IsSuperAdmin(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirst("role_id")?.Value, out var roleId) &&
        SystemRoles.IsSuperAdmin(roleId);
}
