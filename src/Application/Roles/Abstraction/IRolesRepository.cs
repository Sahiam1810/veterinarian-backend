using RoleEntity = HelpDesk.Domain.Roles.Entities.Roles;

namespace HelpDesk.Application.Roles.Abstraction;

public interface IRolesRepository
{
    Task<IReadOnlyCollection<RoleEntity>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<RoleEntity?> GetByIdAsync(
        Guid id,
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