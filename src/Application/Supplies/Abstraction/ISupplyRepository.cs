using Domain.Supplies.Entities;

namespace Application.Supplies.Abstraction;

public interface ISupplyRepository
{
    Task<IEnumerable<Supply>> GetAllAsync(bool onlyActive = true, CancellationToken cancellationToken = default);
    Task<Supply?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Supply supply, CancellationToken cancellationToken = default);
    Task UpdateAsync(Supply supply, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
