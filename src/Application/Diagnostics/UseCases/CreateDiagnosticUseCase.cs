using Application.Diagnostics.Abstraction;
using Domain.Diagnostics.Entities;

namespace Application.Diagnostics.UseCases;

public class CreateDiagnosticUseCase
{
    private readonly IDiagnosticRepository _repository;

    public CreateDiagnosticUseCase(IDiagnosticRepository repository)
    {
        _repository = repository;
    }

    public async Task<Diagnostic> ExecuteAsync(string code, string name, string? description, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpper();

        if (await _repository.ExistsCodeAsync(normalizedCode, cancellationToken))
        {
            throw new InvalidOperationException($"Ya existe un diagnóstico con el código '{normalizedCode}'.");
        }

        var diagnostic = new Diagnostic
        {
            Id = Guid.NewGuid(),
            Code = normalizedCode,
            Name = name.Trim(),
            Description = description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(diagnostic, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return diagnostic;
    }
}
