using Application.Common.Abstractions;
using MediatR;
using ChatEscalationStatusHistoryEntity = Domain.ChatEscalationStatusHistories.Entities.ChatEscalationStatusHistory;

namespace Application.ChatEscalationStatusHistories.UseCase;

public sealed record GetChatEscalationStatusHistoryByIdQuery(Guid Id)
    : IRequest<ChatEscalationStatusHistoryEntity?>;

public sealed class GetChatEscalationStatusHistoryByIdQueryHandler
    : IRequestHandler<GetChatEscalationStatusHistoryByIdQuery, ChatEscalationStatusHistoryEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetChatEscalationStatusHistoryByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<ChatEscalationStatusHistoryEntity?> Handle(
        GetChatEscalationStatusHistoryByIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatEscalationStatusHistoriesRepository.GetByIdAsync(request.Id, cancellationToken);
}
