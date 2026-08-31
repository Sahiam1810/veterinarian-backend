using Application.Agent.Messages;

namespace Application.Agent.Abstractions;

public interface IAgentMessageDispatcher
{
    Task<AgentMessageResult> DispatchAsync(
        AgentMessageDispatchRequest request,
        AgentConversationContext context,
        string accessToken,
        CancellationToken cancellationToken);
}
