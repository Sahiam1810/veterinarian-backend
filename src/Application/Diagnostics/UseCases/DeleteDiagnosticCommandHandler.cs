using Application.Common.Abstractions;
using MediatR;

namespace Application.Diagnostics.UseCases;

public sealed class DeleteDiagnosticCommandHandler
    : IRequestHandler<DeleteDiagnosticCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteDiagnosticCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(
        DeleteDiagnosticCommand request,
        CancellationToken cancellationToken)
    {
        var diagnostic = await _uow.DiagnosticsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (diagnostic is null)
        {
            return false;
        }

        // Baja lógica: se desactiva sin borrar físicamente el registro.
        diagnostic.IsActive = false;
        diagnostic.UpdatedAt = DateTime.UtcNow;

        await _uow.DiagnosticsRepository.UpdateAsync(diagnostic, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
