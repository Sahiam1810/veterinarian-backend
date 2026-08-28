using Application.Agent.Abstractions;
using Application.Agent.Messages;
using Xunit;

namespace Application.Tests.Agent.Messages;

public sealed class SendAgentMessageHandlerTests
{
    private static readonly Guid PersonId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ConversationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CorrelationId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task Handle_builds_envelope_from_authenticated_command_and_resolved_context()
    {
        var conversations = new RecordingConversationContextProvider(
            new AgentConversationContext(ConversationId, "web", false));
        var expected = new AgentMessageResult(
            "Respuesta", ConversationId, CorrelationId, "ai_generated", null);
        var client = new RecordingAgentMessagingClient(expected);
        var handler = new SendAgentMessageHandler(
            conversations,
            new StubUserAccessTokenProvider("signed-access-token"),
            client);

        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.Same(expected, result);
        Assert.Equal(PersonId, client.Envelope!.UserId);
        Assert.Equal(["Cliente"], client.Envelope.Roles);
        Assert.Equal("web", client.Envelope.Channel);
        Assert.False(client.Envelope.IsEscalated);
        Assert.False(client.Envelope.PublishAsGlobalKnowledge);
        Assert.Equal("signed-access-token", client.AccessToken);
    }

    [Fact]
    public async Task Handle_passes_requested_conversation_to_context_provider()
    {
        var requested = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var conversations = new RecordingConversationContextProvider(
            new AgentConversationContext(requested, "web", false));
        var client = new RecordingAgentMessagingClient(
            new AgentMessageResult("Respuesta", requested, CorrelationId, "ai_generated", null));
        var handler = new SendAgentMessageHandler(
            conversations,
            new StubUserAccessTokenProvider("signed-access-token"),
            client);

        await handler.Handle(Command(requested), CancellationToken.None);

        Assert.Equal(PersonId, conversations.PersonId);
        Assert.Equal(requested, conversations.RequestedConversationId);
        Assert.Equal("message-001", conversations.IdempotencyKey);
    }

    [Fact]
    public async Task Handle_propagates_cancellation_to_both_ports()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var conversations = new RecordingConversationContextProvider(
            new AgentConversationContext(ConversationId, "web", false));
        var client = new RecordingAgentMessagingClient(
            new AgentMessageResult("Respuesta", ConversationId, CorrelationId, "ai_generated", null));
        var handler = new SendAgentMessageHandler(
            conversations,
            new StubUserAccessTokenProvider("signed-access-token"),
            client);

        await handler.Handle(Command(), cancellation.Token);

        Assert.Equal(cancellation.Token, conversations.CancellationToken);
        Assert.Equal(cancellation.Token, client.CancellationToken);
    }

    [Fact]
    public async Task Handle_returns_human_controlled_result_without_rewriting_it()
    {
        var expected = new AgentMessageResult(
            null, ConversationId, CorrelationId, "human_controlled", "escalations");
        var handler = new SendAgentMessageHandler(
            new RecordingConversationContextProvider(
                new AgentConversationContext(ConversationId, "web", true)),
            new StubUserAccessTokenProvider("signed-access-token"),
            new RecordingAgentMessagingClient(expected));

        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.Same(expected, result);
    }

    private static SendAgentMessageCommand Command(Guid? conversationId = null) =>
        new(
            "¿Qué vacunas necesita?",
            conversationId,
            null,
            "es-CO",
            PersonId,
            "Cliente",
            "message-001",
            CorrelationId);

    private sealed class RecordingConversationContextProvider(AgentConversationContext result)
        : IConversationContextProvider
    {
        public Guid PersonId { get; private set; }
        public Guid? RequestedConversationId { get; private set; }
        public string? IdempotencyKey { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public ValueTask<AgentConversationContext> ResolveAsync(
            Guid personId,
            Guid? requestedConversationId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            PersonId = personId;
            RequestedConversationId = requestedConversationId;
            IdempotencyKey = idempotencyKey;
            CancellationToken = cancellationToken;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class StubUserAccessTokenProvider(string token) : IUserAccessTokenProvider
    {
        public string GetRequiredAccessToken() => token;
    }

    private sealed class RecordingAgentMessagingClient(AgentMessageResult result)
        : IAgentMessagingClient
    {
        public AgentMessageEnvelope? Envelope { get; private set; }
        public string? AccessToken { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<AgentMessageResult> SendAsync(
            AgentMessageEnvelope message,
            string accessToken,
            CancellationToken cancellationToken)
        {
            Envelope = message;
            AccessToken = accessToken;
            CancellationToken = cancellationToken;
            return Task.FromResult(result);
        }
    }
}
