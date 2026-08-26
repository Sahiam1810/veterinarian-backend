using Application.AiModels.Abstraction;
using MediatR;
using AiModelEntity = Domain.AiModels.Entities.AiModel;

namespace Application.AiModels.UseCase;

public sealed record GetAllAiModelsQuery : IRequest<IReadOnlyCollection<AiModelEntity>>;

public sealed class GetAllAiModelsQueryHandler
    : IRequestHandler<GetAllAiModelsQuery, IReadOnlyCollection<AiModelEntity>>
{
    private readonly IAiModelRepository _repository;

    public GetAllAiModelsQueryHandler(IAiModelRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<AiModelEntity>> Handle(
        GetAllAiModelsQuery request,
        CancellationToken cancellationToken)
        => _repository.GetAllAsync(cancellationToken);
}
