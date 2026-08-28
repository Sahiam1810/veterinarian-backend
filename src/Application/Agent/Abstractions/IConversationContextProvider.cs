using Application.Agent.Messages;

namespace Application.Agent.Abstractions;

public interface IConversationContextProvider
{
    ValueTask<AgentConversationContext> ResolveAsync(
        Guid personId,
        Guid? requestedConversationId,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
