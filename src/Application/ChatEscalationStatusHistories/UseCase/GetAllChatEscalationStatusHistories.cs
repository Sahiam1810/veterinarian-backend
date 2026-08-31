using Application.Common.Abstractions;
using MediatR;
using ChatEscalationStatusHistoryEntity = Domain.ChatEscalationStatusHistories.Entities.ChatEscalationStatusHistory;

namespace Application.ChatEscalationStatusHistories.UseCase;

public sealed record GetAllChatEscalationStatusHistoriesQuery
    : IRequest<IReadOnlyCollection<ChatEscalationStatusHistoryEntity>>;

public sealed class GetAllChatEscalationStatusHistoriesQueryHandler
    : IRequestHandler<GetAllChatEscalationStatusHistoriesQuery, IReadOnlyCollection<ChatEscalationStatusHistoryEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllChatEscalationStatusHistoriesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatEscalationStatusHistoryEntity>> Handle(
        GetAllChatEscalationStatusHistoriesQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatEscalationStatusHistoriesRepository.GetAllAsync(cancellationToken);
}
