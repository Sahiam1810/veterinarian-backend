using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Diagnostics.UseCases;

public sealed class DeleteDiagnosticCommandHandler
    : IRequestHandler<DeleteDiagnosticCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteDiagnosticCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        DeleteDiagnosticCommand request,
        CancellationToken cancellationToken)
    {
        var diagnostic = await _uow.DiagnosticsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException($"Diagnóstico con ID {request.Id} no fue encontrado.");

        // Baja lógica: se desactiva sin borrar físicamente el registro.
        diagnostic.IsActive = false;
        diagnostic.UpdatedAt = DateTime.UtcNow;

        await _uow.DiagnosticsRepository.UpdateAsync(diagnostic, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
