using Domain.TypeServices.Entities;

namespace Application.TypeServices.Abstraction;

public interface ITypeServiceRepository
{
    Task<IReadOnlyCollection<TypeService>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<TypeService?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken,
        Guid? excludedId = null);

    Task AddAsync(
        TypeService typeService,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        TypeService typeService,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        TypeService typeService,
        CancellationToken cancellationToken);
}
