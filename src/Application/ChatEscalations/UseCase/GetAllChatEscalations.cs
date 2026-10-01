using Application.Common.Abstractions;
using MediatR;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;

namespace Application.ChatEscalations.UseCase;

// Ticket B7: escalamiento + prioridad de su conversación — ChatEscalation no
// tiene un campo de prioridad propio, vive en ChatConversation.PriorityId.
public sealed record GetAllChatEscalationsQuery
    : IRequest<IReadOnlyCollection<ChatEscalationEntity>>;

public sealed class GetAllChatEscalationsQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetAllChatEscalationsQuery, IReadOnlyCollection<ChatEscalationEntity>>
{
    public async Task<IReadOnlyCollection<ChatEscalationEntity>> Handle(
        GetAllChatEscalationsQuery request,
        CancellationToken cancellationToken)
    {
        return await uow.ChatEscalationsRepository.GetAllAsync(cancellationToken);
    }
}
