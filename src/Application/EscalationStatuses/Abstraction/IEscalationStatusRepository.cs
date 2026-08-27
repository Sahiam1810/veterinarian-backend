using Domain.EscalationStatuses.Entities;

namespace Application.EscalationStatuses.Abstraction;

// Contrato de persistencia para estados de escalamiento.
public interface IEscalationStatusRepository
{
    Task<IReadOnlyCollection<EscalationStatusEntity>> GetAllAsync(CancellationToken cancellationToken);
    Task<EscalationStatusEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(EscalationStatusEntity escalationStatus, CancellationToken cancellationToken);
    Task UpdateAsync(EscalationStatusEntity escalationStatus, CancellationToken cancellationToken);
    Task DeleteAsync(EscalationStatusEntity escalationStatus, CancellationToken cancellationToken);
}
