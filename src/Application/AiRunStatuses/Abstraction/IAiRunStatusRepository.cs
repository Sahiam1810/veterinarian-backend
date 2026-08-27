using Domain.AiRunStatuses.Entities;

namespace Application.AiRunStatuses.Abstraction;

public interface IAiRunStatusRepository
{
    Task<IReadOnlyCollection<AiRunStatusEntity>> GetAllAsync(CancellationToken cancellationToken);
    Task<AiRunStatusEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(AiRunStatusEntity aiRunStatus, CancellationToken cancellationToken);
    Task UpdateAsync(AiRunStatusEntity aiRunStatus, CancellationToken cancellationToken);
    Task DeleteAsync(AiRunStatusEntity aiRunStatus, CancellationToken cancellationToken);
}
