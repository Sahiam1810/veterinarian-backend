using Api.Common.Security;
using Application.Permissions.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Api.Common.Security.Permissions;

public sealed class PermissionAuthorizationHandler
    (ISender sender) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // El SuperAdmin no participa de la matriz de permisos: se salta todo.
        if (context.User.IsSuperAdmin())
        {
            context.Succeed(requirement);
            return;
        }

        // Los claims agilizan la autenticación, pero no pueden ser la fuente de
        // autorización: un cambio hecho por SuperAdmin debe aplicar de inmediato
        // también a sesiones que ya tenían un JWT emitido.
        if (!Guid.TryParse(context.User.FindFirstValue("role_id"), out var roleId) ||
            !Guid.TryParse(context.User.FindFirstValue("person_id"), out var userId))
        {
            return;
        }

        var permissions = await sender.Send(new GetUserEffectivePermissionsQuery(roleId, userId));
        if (!permissions.TryGetValue(requirement.ModuleName, out var permission))
        {
            return;
        }

        var granted = requirement.Action switch
        {
            PermissionAction.View => permission.CanView,
            PermissionAction.Create => permission.CanCreate,
            PermissionAction.Edit => permission.CanEdit,
            PermissionAction.Delete => permission.CanDelete,
            _ => false,
        };

        if (granted)
        {
            context.Succeed(requirement);
        }
    }
}
