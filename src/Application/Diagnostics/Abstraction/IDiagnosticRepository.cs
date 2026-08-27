using Domain.Diagnostics.Entities;

namespace Application.Diagnostics.Abstraction;

public interface IDiagnosticRepository
{
    Task<IReadOnlyCollection<Diagnostic>> GetAllAsync(bool onlyActive = true, CancellationToken cancellationToken = default);
    Task<Diagnostic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Diagnostic?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(Diagnostic diagnostic, CancellationToken cancellationToken = default);
    Task UpdateAsync(Diagnostic diagnostic, CancellationToken cancellationToken = default);
    Task DeleteAsync(Diagnostic diagnostic, CancellationToken cancellationToken = default);
    Task<bool> ExistsCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> ExistsCodeForDifferentIdAsync(Guid id, string code, CancellationToken cancellationToken = default);
}
