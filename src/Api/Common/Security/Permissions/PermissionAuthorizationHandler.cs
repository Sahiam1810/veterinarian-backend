using Application.Permissions.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Api.Common.Security.Permissions;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly ISender _sender;

    public PermissionAuthorizationHandler(ISender sender)
    {
        _sender = sender;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // El SuperAdmin no participa de la matriz de permisos: se salta todo.
        if (context.User.HasClaim(claim => claim.Type == "super_admin" && claim.Value == "true"))
        {
            context.Succeed(requirement);
            return;
        }

        var roleIdClaim = context.User.FindFirst("role_id")?.Value;

        if (!Guid.TryParse(roleIdClaim, out var roleId))
        {
            return;
        }

        // El permiso puntual por usuario (person_id) es opcional: si no viene
        // o no hay fila para ese usuario, el permiso efectivo queda igual al del rol.
        Guid.TryParse(context.User.FindFirst("person_id")?.Value, out var userId);

        var permission = await _sender.Send(
            new GetEffectivePermissionQuery(roleId, userId, requirement.ModuleName));

        var granted = requirement.Action switch
        {
            PermissionAction.View => permission.CanView,
            PermissionAction.Create => permission.CanCreate,
            PermissionAction.Edit => permission.CanEdit,
            PermissionAction.Delete => permission.CanDelete,
            _ => false
        };

        if (granted)
        {
            context.Succeed(requirement);
        }
    }
}
