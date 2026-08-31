using Application.Agent.Abstractions;
using Application.Agent.Errors;
using Application.Agent.Messages;

namespace Infrastructure.Agent.Conversations;

public sealed class DisabledConversationContextProvider : IConversationContextProvider
{
    public ValueTask<AgentConversationContext> ResolveAsync(
        Guid personId,
        Guid? requestedConversationId,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        ValueTask.FromException<AgentConversationContext>(
            new AgentUnavailableException());
}
