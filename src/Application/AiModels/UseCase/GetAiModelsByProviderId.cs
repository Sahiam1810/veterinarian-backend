using Application.Common.Abstractions;
using MediatR;
using AiModelEntity = Domain.AiModels.Entities.AiModel;

namespace Application.AiModels.UseCase;

public sealed record GetAiModelsByProviderIdQuery(Guid ProviderModelAiId)
    : IRequest<IReadOnlyCollection<AiModelEntity>>;

public sealed class GetAiModelsByProviderIdQueryHandler
    : IRequestHandler<GetAiModelsByProviderIdQuery, IReadOnlyCollection<AiModelEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAiModelsByProviderIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<AiModelEntity>> Handle(
        GetAiModelsByProviderIdQuery request,
        CancellationToken cancellationToken)
        => _uow.AiModelsRepository.GetByProviderIdAsync(request.ProviderModelAiId, cancellationToken);
}
