using AiModelEntity = Domain.AiModels.Entities.AiModel;

namespace Application.AiModels.Abstraction;

public interface IAiModelRepository
{
    Task<IReadOnlyCollection<AiModelEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AiModelEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AiModelEntity>> GetByProviderIdAsync(
        Guid providerModelAiId,
        CancellationToken cancellationToken = default);

    Task AddAsync(AiModelEntity model, CancellationToken cancellationToken = default);

    Task UpdateAsync(AiModelEntity model, CancellationToken cancellationToken = default);
}
