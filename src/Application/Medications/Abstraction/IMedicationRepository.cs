using Domain.Medications.Entities;

namespace Application.Medications.Abstraction;

public interface IMedicationRepository
{
    Task<IEnumerable<Medication>> GetAllAsync(bool onlyActive = true, CancellationToken cancellationToken = default);
    Task<Medication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(
        string? code,
        CancellationToken cancellationToken = default,
        Guid? excludedId = null);
    Task AddAsync(Medication medication, CancellationToken cancellationToken = default);
    Task UpdateAsync(Medication medication, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
