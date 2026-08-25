using HelpDesk.Api.Roles.Dtos;
using HelpDesk.Application.Roles.UseCase;
using RoleEntity = HelpDesk.Domain.Roles.Entities.Roles;

namespace HelpDesk.Api.Roles.Mappings;

public static class RolesMappings
{
    public static CreateRoleCommand ToCommand(
        this CreateRoleRequest request)
    {
        return new CreateRoleCommand(
            request.Name,
            request.Description);
    }

    public static UpdateRoleCommand ToCommand(
        this UpdateRoleRequest request,
        Guid id)
    {
        return new UpdateRoleCommand(
            id,
            request.Name,
            request.Description);
    }

    public static RoleResponse ToResponse(this RoleEntity role)
    {
        return new RoleResponse(
            role.Id,
            role.Name.Value,
            role.Description,
            role.CreatedAt);
    }

    public static IReadOnlyCollection<RoleResponse> ToResponse(
        this IReadOnlyCollection<RoleEntity> roles)
    {
        return roles
            .Select(role => role.ToResponse())
            .ToArray();
    }
}
