using Application.Agent.Abstractions;
using MediatR;

namespace Application.Agent.Messages;

public sealed class SendAgentMessageHandler(
    IConversationContextProvider conversationContextProvider,
    IUserAccessTokenProvider userAccessTokenProvider,
    IAgentMessagingClient agentMessagingClient)
    : IRequestHandler<SendAgentMessageCommand, AgentMessageResult>
{
    public async Task<AgentMessageResult> Handle(
        SendAgentMessageCommand request,
        CancellationToken cancellationToken)
    {
        var context = await conversationContextProvider.ResolveAsync(
            request.PersonId,
            request.ConversationId,
            request.IdempotencyKey,
            cancellationToken);
        var accessToken = userAccessTokenProvider.GetRequiredAccessToken();
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

        return await agentMessagingClient.SendAsync(
            envelope,
            accessToken,
            cancellationToken);
    }
}
