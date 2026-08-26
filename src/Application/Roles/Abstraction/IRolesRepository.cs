using RoleEntity = Domain.Roles.Entities.Roles;

namespace Application.Roles.Abstraction;

public interface IRolesRepository
{
    Task<IReadOnlyCollection<RoleEntity>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<RoleEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<RoleEntity?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken,
        Guid? excludedId = null);

    Task AddAsync(
        RoleEntity role,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        RoleEntity role,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        RoleEntity role,
        CancellationToken cancellationToken);
}