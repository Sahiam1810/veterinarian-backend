using Application.AiModels.Abstraction;
using MediatR;
using AiModelEntity = Domain.AiModels.Entities.AiModel;

namespace Application.AiModels.UseCase;

public sealed record GetAiModelByIdQuery(Guid Id) : IRequest<AiModelEntity?>;

public sealed class GetAiModelByIdQueryHandler
    : IRequestHandler<GetAiModelByIdQuery, AiModelEntity?>
{
    private readonly IAiModelRepository _repository;

    public GetAiModelByIdQueryHandler(IAiModelRepository repository)
    {
        _repository = repository;
    }

    public Task<AiModelEntity?> Handle(
        GetAiModelByIdQuery request,
        CancellationToken cancellationToken)
        => _repository.GetByIdAsync(request.Id, cancellationToken);
}
