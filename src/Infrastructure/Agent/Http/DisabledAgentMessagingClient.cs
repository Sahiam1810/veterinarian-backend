using Application.Agent.Abstractions;
using Application.Agent.Errors;
using Application.Agent.Messages;

namespace Infrastructure.Agent.Http;

public sealed class DisabledAgentMessagingClient : IAgentMessagingClient
{
    public Task<AgentMessageResult> SendAsync(
        AgentMessageEnvelope message,
        string accessToken,
        CancellationToken cancellationToken) =>
        Task.FromException<AgentMessageResult>(new AgentUnavailableException());
}
