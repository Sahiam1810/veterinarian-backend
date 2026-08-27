using Application.Common.Abstractions;
using Domain.Diagnostics.Entities;
using MediatR;

namespace Application.Diagnostics.UseCases;

public sealed class GetAllDiagnosticsQueryHandler
    : IRequestHandler<GetAllDiagnosticsQuery, IReadOnlyCollection<Diagnostic>>
{
    private readonly IUnitOfWork _uow;

    public GetAllDiagnosticsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<Diagnostic>> Handle(
        GetAllDiagnosticsQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.DiagnosticsRepository.GetAllAsync(
            request.OnlyActive,
            cancellationToken);
    }
}
