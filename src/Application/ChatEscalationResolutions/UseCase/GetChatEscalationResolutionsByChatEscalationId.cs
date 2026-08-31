using Application.Common.Abstractions;
using MediatR;
using ChatEscalationResolutionEntity = Domain.ChatEscalationResolutions.Entities.ChatEscalationResolution;

namespace Application.ChatEscalationResolutions.UseCase;

public sealed record GetChatEscalationResolutionsByChatEscalationIdQuery(Guid ChatEscalationId)
    : IRequest<IReadOnlyCollection<ChatEscalationResolutionEntity>>;

public sealed class GetChatEscalationResolutionsByChatEscalationIdQueryHandler
    : IRequestHandler<GetChatEscalationResolutionsByChatEscalationIdQuery, IReadOnlyCollection<ChatEscalationResolutionEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetChatEscalationResolutionsByChatEscalationIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatEscalationResolutionEntity>> Handle(
        GetChatEscalationResolutionsByChatEscalationIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatEscalationResolutionsRepository.GetByChatEscalationIdAsync(
            request.ChatEscalationId,
            cancellationToken);
}
