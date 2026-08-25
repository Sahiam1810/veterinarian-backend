using Application.Diagnostics.Abstraction;
using Domain.Diagnostics.Entities;

namespace Application.Diagnostics.UseCases;

public class DeleteDiagnosticUseCase
{
    private readonly IDiagnosticRepository _repository;
    public DeleteDiagnosticUseCase(IDiagnosticRepository repository)
    {
        _repository = repository;
    }
    public async Task<bool> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var diagnostic = await _repository.GetByIdAsync(id, cancellationToken);
        if (diagnostic == null) return false;
        // Soft delete: desactivar sin borrar físicamente
        diagnostic.IsActive = false;
        diagnostic.UpdatedAt = DateTime.UtcNow;
        _repository.Update(diagnostic);
        await _repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
