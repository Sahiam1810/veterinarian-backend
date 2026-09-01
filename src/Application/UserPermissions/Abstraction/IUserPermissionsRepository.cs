using UserPermissionEntity = Domain.UserPermissions.Entities.UserPermission;

namespace Application.UserPermissions.Abstraction;

public interface IUserPermissionsRepository
{
    Task<IReadOnlyCollection<UserPermissionEntity>> GetAllAsync(CancellationToken cancellationToken);

    Task<UserPermissionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserPermissionEntity>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<UserPermissionEntity?> GetByUserAndModuleIdAsync(
        Guid userId,
        Guid moduleId,
        CancellationToken cancellationToken);

    Task<UserPermissionEntity?> GetByUserAndModuleNameAsync(
        Guid userId,
        string moduleName,
        CancellationToken cancellationToken);

    Task AddAsync(UserPermissionEntity permission, CancellationToken cancellationToken);

    Task UpdateAsync(UserPermissionEntity permission, CancellationToken cancellationToken);

    Task DeleteAsync(UserPermissionEntity permission, CancellationToken cancellationToken);
}
