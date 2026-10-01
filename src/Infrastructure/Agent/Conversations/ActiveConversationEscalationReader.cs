using Application.Agent.Abstractions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;

namespace Infrastructure.Agent.Conversations;

public sealed class ActiveConversationEscalationReader(
    VeterinaryDbContext context) : IActiveConversationEscalationReader
{
    public Task<bool> HasActiveAsync(
        Guid conversationId,
        CancellationToken cancellationToken) =>
        context.Set<ChatEscalationEntity>()
            .AsNoTracking()
            .AnyAsync(
                escalation =>
                    escalation.ChatConversationId == conversationId &&
                    escalation.ResolvedAt == null,
                cancellationToken);
}
