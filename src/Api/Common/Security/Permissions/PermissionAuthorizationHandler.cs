using Api.Common.Security;
using Application.Permissions.Claims;
using Application.Permissions.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Api.Common.Security.Permissions;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ISender? _sender;

    public PermissionAuthorizationHandler() : this(null)
    {
    }

    public PermissionAuthorizationHandler(ISender? sender)
    {
        _sender = sender;
    }

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

        var requiredPermission = PermissionClaimValue.Create(
            requirement.ModuleName,
            requirement.Action.ToString());

        // 1. Verificación rápida desde claims del JWT (O(1))
        if (context.User.HasClaim(
                PermissionClaimValue.ClaimType,
                requiredPermission))
        {
            context.Succeed(requirement);
            return;
        }

        // 2. Consulta dinámica a base de datos si el token no tiene el claim
        // (ej. permisos recién asignados por SuperAdmin sin re-login)
        try
        {
            if (_sender != null &&
                Guid.TryParse(context.User.FindFirstValue("role_id"), out var roleId) &&
                Guid.TryParse(context.User.FindFirstValue("person_id"), out var userId))
            {
                var permissions = await _sender.Send(new GetUserEffectivePermissionsQuery(roleId, userId));
                if (permissions != null && permissions.TryGetValue(requirement.ModuleName, out var permission))
                {
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
        }
        catch
        {
            // Falla cerrado ante error de base de datos o en entornos de prueba sin conexión.
        }
    }
}

