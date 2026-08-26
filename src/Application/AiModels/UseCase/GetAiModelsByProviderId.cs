using Application.AiModels.Abstraction;
using MediatR;
using AiModelEntity = Domain.AiModels.Entities.AiModel;

namespace Application.AiModels.UseCase;

public sealed record GetAiModelsByProviderIdQuery(Guid ProviderModelAiId)
    : IRequest<IReadOnlyCollection<AiModelEntity>>;

public sealed class GetAiModelsByProviderIdQueryHandler
    : IRequestHandler<GetAiModelsByProviderIdQuery, IReadOnlyCollection<AiModelEntity>>
{
    private readonly IAiModelRepository _repository;

    public GetAiModelsByProviderIdQueryHandler(IAiModelRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<AiModelEntity>> Handle(
        GetAiModelsByProviderIdQuery request,
        CancellationToken cancellationToken)
        => _repository.GetByProviderIdAsync(request.ProviderModelAiId, cancellationToken);
}
