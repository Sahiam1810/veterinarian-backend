using veterinarian_backend.Domain.Races.Entities;

namespace Application.Races.Abstraction;

public interface IRaceRepository
{
    Task<IReadOnlyCollection<RaceEntity>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<RaceEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        string name,
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
