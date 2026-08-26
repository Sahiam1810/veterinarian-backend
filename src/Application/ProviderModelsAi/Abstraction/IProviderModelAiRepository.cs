using ProviderEntity = Domain.ProviderModelsAi.Entities.ProviderModelAi;

namespace Application.ProviderModelsAi.Abstraction;

public interface IProviderModelAiRepository
{
    Task<IReadOnlyCollection<ProviderEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ProviderEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(ProviderEntity provider, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProviderEntity provider, CancellationToken cancellationToken = default);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
