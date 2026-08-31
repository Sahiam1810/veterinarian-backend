using Application.Agent.Abstractions;

namespace Application.Agent.Messages;

public sealed class AgentMessageDispatcher(IAgentMessagingClient agentMessagingClient)
    : IAgentMessageDispatcher
{
    public Task<AgentMessageResult> DispatchAsync(
        AgentMessageDispatchRequest request,
        AgentConversationContext context,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var envelope = new AgentMessageEnvelope(
            request.Message,
            context.ConversationId,
            request.PersonId,
            request.PetId,
            context.Channel,
            request.Language,
            [request.Role],
            context.IsEscalated,
            request.CorrelationId,
            request.IdempotencyKey,
            false);

        return agentMessagingClient.SendAsync(
            envelope,
            accessToken,
            cancellationToken);
    }
}
