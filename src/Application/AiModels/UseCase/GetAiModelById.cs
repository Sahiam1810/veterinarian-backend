using Application.Common.Abstractions;
using MediatR;
using AiModelEntity = Domain.AiModels.Entities.AiModel;

namespace Application.AiModels.UseCase;

public sealed record GetAiModelByIdQuery(Guid Id) : IRequest<AiModelEntity?>;

public sealed class GetAiModelByIdQueryHandler
    : IRequestHandler<GetAiModelByIdQuery, AiModelEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetAiModelByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<AiModelEntity?> Handle(
        GetAiModelByIdQuery request,
        CancellationToken cancellationToken)
        => _uow.AiModelsRepository.GetByIdAsync(request.Id, cancellationToken);
}
