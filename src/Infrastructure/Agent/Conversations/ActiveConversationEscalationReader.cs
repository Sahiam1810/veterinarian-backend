using Application.Agent.Abstractions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;
using ChatEscalationResolutionEntity = Domain.ChatEscalationResolutions.Entities.ChatEscalationResolution;

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
                    !context.Set<ChatEscalationResolutionEntity>().Any(
                        resolution =>
                            resolution.ChatEscalationId == escalation.Id),
                cancellationToken);
}
