using Domain.Modules.Entities;

namespace Application.Modules.Abstraction;

public interface IModuleRepository
{
    Task AddAsync(ModuleEntity module, CancellationToken cancellationToken);

    Task<ModuleEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ModuleEntity?> GetByNameAsync(string name, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ModuleEntity>> GetAllAsync(CancellationToken cancellationToken);

    Task UpdateAsync(ModuleEntity module, CancellationToken cancellationToken);

    Task DeleteAsync(ModuleEntity module, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken,
        Guid? excludedId = null);
}
