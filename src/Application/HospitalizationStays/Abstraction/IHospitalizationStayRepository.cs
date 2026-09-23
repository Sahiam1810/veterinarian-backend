using Domain.HospitalizationStays.Entities;

namespace Application.HospitalizationStays.Abstraction;

public interface IHospitalizationStayRepository
{
    Task<HospitalizationStay?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<HospitalizationStay?> GetActiveByPetIdAsync(
        Guid clientPetId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<HospitalizationStay>> GetAllActiveAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<HospitalizationStay>> GetByClientPetIdAsync(
        Guid clientPetId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        HospitalizationStay stay,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        HospitalizationStay stay,
        CancellationToken cancellationToken = default);
}
