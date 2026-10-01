using Application.Permissions.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Api.Common.Security.Permissions;

// El nombre de policy se arma como "perm:{módulo}:{acción}" y lo resuelve
// PermissionPolicyProvider al vuelo (no hace falta registrar una policy
// distinta por cada combinación de módulo y acción).
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = PermissionClaimValue.PolicyPrefix;

    public RequirePermissionAttribute(string moduleName, PermissionAction action)
    {
        Policy = $"{PolicyPrefix}{moduleName}:{action}";
    }
}
