using Application.RolePermissions.UseCases;
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
        var roleIdClaim = context.User.FindFirst("role_id")?.Value;

        if (!Guid.TryParse(roleIdClaim, out var roleId))
        {
            return;
        }

        var permission = await _sender.Send(
            new GetRolePermissionQuery(roleId, requirement.ModuleName));

        if (permission is null)
        {
            return;
        }

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
