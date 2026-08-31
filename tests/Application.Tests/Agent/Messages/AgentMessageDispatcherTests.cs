using Application.Agent.Abstractions;
using Application.Agent.Messages;
using Xunit;

namespace Application.Tests.Agent.Messages;

public sealed class AgentMessageDispatcherTests
{
    [Fact]
    public async Task Dispatch_builds_telegram_envelope_from_resolved_context()
    {
        var conversationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var personId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var correlationId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var expected = new AgentMessageResult(
            "respuesta",
            conversationId,
            correlationId,
            "ai_generated",
            "openai",
            "gpt-4o-mini",
            null,
            null,
            new AgentRagResult("used", "contextual", 0.9, 1, 1, true, false));
        var client = new RecordingClient(expected);
        var dispatcher = new AgentMessageDispatcher(client);

        var result = await dispatcher.DispatchAsync(
            new AgentMessageDispatchRequest(
                "hola",
                personId,
                null,
                "es-CO",
                "Cliente",
                "telegram-update-42",
                correlationId),
            new AgentConversationContext(conversationId, "telegram", true),
            "delegated-token",
            CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal("telegram", client.Envelope!.Channel);
        Assert.True(client.Envelope.IsEscalated);
        Assert.Equal(personId, client.Envelope.UserId);
        Assert.Equal("delegated-token", client.AccessToken);
    }

    private sealed class RecordingClient(AgentMessageResult result)
        : IAgentMessagingClient
    {
        public AgentMessageEnvelope? Envelope { get; private set; }
        public string? AccessToken { get; private set; }

        public Task<AgentMessageResult> SendAsync(
            AgentMessageEnvelope message,
            string accessToken,
            CancellationToken cancellationToken)
        {
            Envelope = message;
            AccessToken = accessToken;
            return Task.FromResult(result);
        }
    }
}
