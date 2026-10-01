using Domain.HospitalizationStays.Entities;

namespace Application.HospitalizationStays.Abstraction;

public interface IHospitalizationNoteRepository
{
    Task<IReadOnlyCollection<HospitalizationNote>> GetByStayIdAsync(
        Guid stayId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        HospitalizationNote note,
        CancellationToken cancellationToken = default);
}
