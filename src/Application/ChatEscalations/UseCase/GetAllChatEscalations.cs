using Application.Common.Abstractions;
using MediatR;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;

namespace Application.ChatEscalations.UseCase;

// Ticket B7: escalamiento + prioridad de su conversación — ChatEscalation no
// tiene un campo de prioridad propio, vive en ChatConversation.PriorityId.
public sealed record ChatEscalationWithPriority(
    ChatEscalationEntity Escalation,
    Guid? PriorityId,
    string? Priority);

public sealed record GetAllChatEscalationsQuery
    : IRequest<IReadOnlyCollection<ChatEscalationWithPriority>>;

public sealed class GetAllChatEscalationsQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetAllChatEscalationsQuery, IReadOnlyCollection<ChatEscalationWithPriority>>
{
    public async Task<IReadOnlyCollection<ChatEscalationWithPriority>> Handle(
        GetAllChatEscalationsQuery request,
        CancellationToken cancellationToken)
    {
        var escalations = await uow.ChatEscalationsRepository.GetAllAsync(cancellationToken);

        var results = new List<ChatEscalationWithPriority>(escalations.Count);
        foreach (var escalation in escalations)
        {
            Guid? priorityId = null;
            string? priorityName = null;

            var conversation = await uow.ChatConversationsRepository.GetByIdAsync(
                escalation.ChatConversationId, cancellationToken);
            if (conversation?.PriorityId is { } id)
            {
                priorityId = id;
                var priority = await uow.PrioritiesRepository.GetByIdAsync(id, cancellationToken);
                priorityName = priority?.Name.Value;
            }

            results.Add(new ChatEscalationWithPriority(escalation, priorityId, priorityName));
        }

        return results;
    }
}
