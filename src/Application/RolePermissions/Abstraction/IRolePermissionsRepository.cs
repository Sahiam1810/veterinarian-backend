using RolePermissionEntity = Domain.RolePermissions.Entities.RolePermission;

namespace Application.RolePermissions.Abstraction;

public interface IRolePermissionsRepository
{
    Task<RolePermissionEntity?> GetByRoleAndModuleNameAsync(
        Guid roleId,
        string moduleName,
        CancellationToken cancellationToken);
}
