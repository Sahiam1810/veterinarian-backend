using Domain.Races.Entities;

namespace Application.Races.Abstraction;

public interface IRaceRepository
{
    Task<IReadOnlyCollection<RaceEntity>> GetAllAsync(
        CancellationToken cancellationToken);

    // Razas de una especie (para el combo del formulario)
    Task<IReadOnlyCollection<RaceEntity>> GetBySpeciesIdAsync(
        Guid speciesId,
        CancellationToken cancellationToken);

    Task<RaceEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameInSpeciesAsync(
        string name,
        Guid speciesId,
        CancellationToken cancellationToken,
        Guid? excludedId = null);

    Task AddAsync(
        RaceEntity race,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        RaceEntity race,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        RaceEntity race,
        CancellationToken cancellationToken);
}
