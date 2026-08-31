using ModuleEntity = Domain.Modules.Entities.ModuleEntity;

namespace Application.Modules.Abstraction;

// Contrato de persistencia para módulos del sistema.
public interface IModulesRepository
{
    Task<IReadOnlyCollection<ModuleEntity>> GetAllAsync(CancellationToken cancellationToken);

    Task<ModuleEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ModuleEntity?> GetByNameAsync(string name, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    Task AddAsync(ModuleEntity module, CancellationToken cancellationToken);

    Task UpdateAsync(ModuleEntity module, CancellationToken cancellationToken);

    Task DeleteAsync(ModuleEntity module, CancellationToken cancellationToken);
}
