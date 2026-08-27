using Application.Common.Abstractions;
using MediatR;
using ProviderEntity = Domain.ProviderModelsAi.Entities.ProviderModelAi;

namespace Application.ProviderModelsAi.UseCase;

public sealed record GetProviderModelAiByIdQuery(Guid Id) : IRequest<ProviderEntity?>;

public sealed class GetProviderModelAiByIdQueryHandler
    : IRequestHandler<GetProviderModelAiByIdQuery, ProviderEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetProviderModelAiByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<ProviderEntity?> Handle(
        GetProviderModelAiByIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ProviderModelsAiRepository.GetByIdAsync(request.Id, cancellationToken);
}
