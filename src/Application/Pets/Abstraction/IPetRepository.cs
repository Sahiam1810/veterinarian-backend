using Domain.Pets.Entities;

namespace Application.Pets.Abstraction;

public interface IPetRepository
{
    Task<IReadOnlyCollection<PetEntity>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<PetEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PetEntity>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    Task AddAsync(
        PetEntity pet,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        PetEntity pet,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        PetEntity pet,
        CancellationToken cancellationToken);
}
