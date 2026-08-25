using Application.Diagnostics.Abstraction;
using Domain.Diagnostics.Entities;

namespace Application.Diagnostics.UseCases;

public class GetDiagnosticByIdUseCase
{
    private readonly IDiagnosticRepository _repository;

    public GetDiagnosticByIdUseCase(IDiagnosticRepository repository)
    {
        _repository = repository;
    }

    public async Task<Diagnostic?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }
}