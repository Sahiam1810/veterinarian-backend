using MediatR;
using RolePermissionEntity = Domain.RolePermissions.Entities.RolePermission;

namespace Application.RolePermissions.UseCases;

public sealed record GetRolePermissionQuery(Guid RoleId, string ModuleName)
    : IRequest<RolePermissionEntity?>;
