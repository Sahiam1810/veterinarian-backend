using Application.Diagnostics.Abstraction;
using Domain.Diagnostics.Entities;

namespace Application.Diagnostics.UseCases;

public class UpdateDiagnosticUseCase
{
    private readonly IDiagnosticRepository _repository;
    public UpdateDiagnosticUseCase(IDiagnosticRepository repository)
    {
        _repository = repository;
    }
    public async Task<Diagnostic?> ExecuteAsync(Guid id, string code, string name, string? description, bool isActive, CancellationToken cancellationToken = default)
    {
        var diagnostic = await _repository.GetByIdAsync(id, cancellationToken);
        if (diagnostic == null) return null;
        var normalizedCode = code.Trim().ToUpper();
        if (await _repository.ExistsCodeForDifferentIdAsync(id, normalizedCode, cancellationToken))
        {
            throw new InvalidOperationException($"Ya existe otro diagnóstico con el código '{normalizedCode}'.");
        }
        diagnostic.Code = normalizedCode;
        diagnostic.Name = name.Trim();
        diagnostic.Description = description?.Trim();
        diagnostic.IsActive = isActive;
        diagnostic.UpdatedAt = DateTime.UtcNow;
        _repository.Update(diagnostic);
        await _repository.SaveChangesAsync(cancellationToken);
        return diagnostic;
    }
}