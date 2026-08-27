using Application.Common.Abstractions;
using MediatR;
using AiModelEntity = Domain.AiModels.Entities.AiModel;

namespace Application.AiModels.UseCase;

public sealed record GetAllAiModelsQuery : IRequest<IReadOnlyCollection<AiModelEntity>>;

public sealed class GetAllAiModelsQueryHandler
    : IRequestHandler<GetAllAiModelsQuery, IReadOnlyCollection<AiModelEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllAiModelsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<AiModelEntity>> Handle(
        GetAllAiModelsQuery request,
        CancellationToken cancellationToken)
        => _uow.AiModelsRepository.GetAllAsync(cancellationToken);
}
