using Application.Common.Abstractions;
using MediatR;
using ChatEscalationStatusHistoryEntity = Domain.ChatEscalationStatusHistories.Entities.ChatEscalationStatusHistory;

namespace Application.ChatEscalationStatusHistories.UseCase;

public sealed record GetChatEscalationStatusHistoriesByChatEscalationIdQuery(Guid ChatEscalationId)
    : IRequest<IReadOnlyCollection<ChatEscalationStatusHistoryEntity>>;

public sealed class GetChatEscalationStatusHistoriesByChatEscalationIdQueryHandler
    : IRequestHandler<GetChatEscalationStatusHistoriesByChatEscalationIdQuery, IReadOnlyCollection<ChatEscalationStatusHistoryEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetChatEscalationStatusHistoriesByChatEscalationIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatEscalationStatusHistoryEntity>> Handle(
        GetChatEscalationStatusHistoriesByChatEscalationIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatEscalationStatusHistoriesRepository.GetByChatEscalationIdAsync(
            request.ChatEscalationId,
            cancellationToken);
}
