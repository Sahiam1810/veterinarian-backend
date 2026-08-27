using Application.Common.Abstractions;
using Domain.Diagnostics.Entities;
using MediatR;

namespace Application.Diagnostics.UseCases;

public sealed class GetDiagnosticByIdQueryHandler
    : IRequestHandler<GetDiagnosticByIdQuery, Diagnostic?>
{
    private readonly IUnitOfWork _uow;

    public GetDiagnosticByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Diagnostic?> Handle(
        GetDiagnosticByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.DiagnosticsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
    }
}
