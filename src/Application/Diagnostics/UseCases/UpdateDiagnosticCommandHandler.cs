using Application.Common.Abstractions;
using Domain.Diagnostics.Entities;
using MediatR;

namespace Application.Diagnostics.UseCases;

public sealed class UpdateDiagnosticCommandHandler
    : IRequestHandler<UpdateDiagnosticCommand, Diagnostic?>
{
    private readonly IUnitOfWork _uow;

    public UpdateDiagnosticCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Diagnostic?> Handle(
        UpdateDiagnosticCommand request,
        CancellationToken cancellationToken)
    {
        var diagnostic = await _uow.DiagnosticsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (diagnostic is null)
        {
            return null;
        }

        diagnostic.Code = request.Code.Trim().ToUpper();
        diagnostic.Name = request.Name.Trim();
        diagnostic.Description = request.Description?.Trim();
        diagnostic.IsActive = request.IsActive;
        diagnostic.UpdatedAt = DateTime.UtcNow;

        await _uow.DiagnosticsRepository.UpdateAsync(diagnostic, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return diagnostic;
    }
}
