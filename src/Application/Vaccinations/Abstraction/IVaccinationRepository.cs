using Domain.Vaccinations.Entities;

namespace Application.Vaccinations.Abstraction;

public interface IVaccinationRepository
{
    Task<IReadOnlyCollection<Vaccination>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<Vaccination?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task AddAsync(
        Vaccination vaccination,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        Vaccination vaccination,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Vaccination vaccination,
        CancellationToken cancellationToken);
}
