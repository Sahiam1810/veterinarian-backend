using RolePermissionEntity = Domain.RolePermissions.Entities.RolePermission;

namespace Application.RolePermissions.Abstraction;

// Contrato de persistencia para permisos por rol y módulo.
public interface IRolePermissionsRepository
{
    Task<IReadOnlyCollection<RolePermissionEntity>> GetAllAsync(CancellationToken cancellationToken);

    Task<RolePermissionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RolePermissionEntity>> GetByRoleIdAsync(
        Guid roleId,
        CancellationToken cancellationToken);

    Task<RolePermissionEntity?> GetByRoleAndModuleIdAsync(
        Guid roleId,
        Guid moduleId,
        CancellationToken cancellationToken);

    Task<RolePermissionEntity?> GetByRoleAndModuleNameAsync(
        Guid roleId,
        string moduleName,
        CancellationToken cancellationToken);

    Task AddAsync(RolePermissionEntity permission, CancellationToken cancellationToken);

    Task UpdateAsync(RolePermissionEntity permission, CancellationToken cancellationToken);

    Task DeleteAsync(RolePermissionEntity permission, CancellationToken cancellationToken);
}
