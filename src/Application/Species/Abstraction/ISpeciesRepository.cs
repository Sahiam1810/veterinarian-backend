using Domain.Species.Entities;

namespace Application.Species.Abstraction;

public interface ISpeciesRepository
{
    Task<IReadOnlyCollection<SpeciesEntity>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<SpeciesEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken,
        Guid? excludedId = null);

    Task AddAsync(
        SpeciesEntity species,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        SpeciesEntity species,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        SpeciesEntity species,
        CancellationToken cancellationToken);
}
