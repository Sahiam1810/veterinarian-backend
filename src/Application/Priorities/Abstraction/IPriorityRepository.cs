using Domain.Priorities.Entities;

namespace Application.Priorities.Abstraction;

// Contrato de persistencia para prioridades.
public interface IPriorityRepository
{
    Task<IReadOnlyCollection<PriorityEntity>> GetAllAsync(CancellationToken cancellationToken);
    Task<PriorityEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(PriorityEntity priority, CancellationToken cancellationToken);
    Task UpdateAsync(PriorityEntity priority, CancellationToken cancellationToken);
    Task DeleteAsync(PriorityEntity priority, CancellationToken cancellationToken);
}
