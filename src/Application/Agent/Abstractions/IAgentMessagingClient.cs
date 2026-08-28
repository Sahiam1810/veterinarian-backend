using Application.Agent.Messages;

namespace Application.Agent.Abstractions;

public interface IAgentMessagingClient
{
    Task<AgentMessageResult> SendAsync(
        AgentMessageEnvelope message,
        string accessToken,
        CancellationToken cancellationToken);
}
