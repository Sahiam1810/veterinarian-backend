using Domain.VeterinarianAbsences.Entities;

namespace Application.VeterinarianAbsences.Abstraction;

public interface IVeterinarianAbsenceRepository
{
    Task<VeterinarianAbsence?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<VeterinarianAbsence>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<VeterinarianAbsence>> GetAllByVeterinarianIdAsync(
        Guid veterinarianId,
        CancellationToken cancellationToken);

    // Cruce inclusivo: start < to && end > from.
    Task<IReadOnlyCollection<VeterinarianAbsence>> GetOverlappingAsync(
        Guid veterinarianId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);

    Task AddAsync(
        VeterinarianAbsence absence,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        VeterinarianAbsence absence,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        VeterinarianAbsence absence,
        CancellationToken cancellationToken);
}
