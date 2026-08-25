using Application.Diagnostics.Abstraction;
using Domain.Diagnostics.Entities;

namespace Application.Diagnostics.UseCases;
public class GetAllDiagnosticsUseCase
{
    private readonly IDiagnosticRepository _repository;

    public GetAllDiagnosticsUseCase(IDiagnosticRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Diagnostic>> ExecuteAsync(bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllAsync(onlyActive, cancellationToken);
    }

}
