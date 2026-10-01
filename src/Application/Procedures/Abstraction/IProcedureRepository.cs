using Domain.Procedures.Entities;

namespace Application.Procedures.Abstraction;

public interface IProcedureRepository
{
    Task<IEnumerable<Procedure>> GetAllAsync(bool onlyActive = true, CancellationToken cancellationToken = default);
    Task<Procedure?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Procedure procedure, CancellationToken cancellationToken = default);
    Task UpdateAsync(Procedure procedure, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
