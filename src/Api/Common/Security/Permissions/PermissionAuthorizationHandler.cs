using Api.Common.Security;
using Application.Permissions.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Api.Common.Security.Permissions;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // El SuperAdmin no participa de la matriz de permisos: se salta todo.
        if (context.User.IsSuperAdmin())
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var requiredPermission = PermissionClaimValue.Create(
            requirement.ModuleName,
            requirement.Action.ToString());
        if (context.User.HasClaim(
                PermissionClaimValue.ClaimType,
                requiredPermission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
