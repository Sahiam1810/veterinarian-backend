using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Diagnostics.Entities;
using MediatR;

namespace Application.Diagnostics.UseCases;

public sealed class GetDiagnosticByIdQueryHandler
    : IRequestHandler<GetDiagnosticByIdQuery, Diagnostic>
{
    private readonly IUnitOfWork _uow;

    public GetDiagnosticByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Diagnostic> Handle(
        GetDiagnosticByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.DiagnosticsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException($"Diagnóstico con ID {request.Id} no fue encontrado.");
    }
}
