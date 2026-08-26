using Domain.Services.Entities;

namespace Application.Services.Abstraction;

public interface IServiceRepository
{
    Task<IReadOnlyCollection<Service>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<Service?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken,
        Guid? excludedId = null);

    Task AddAsync(
        Service service,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        Service service,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Service service,
        CancellationToken cancellationToken);
}
