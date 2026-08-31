using Application.Agent.Abstractions;
using MediatR;

namespace Application.Agent.Messages;

public sealed class SendAgentMessageHandler(
    IConversationContextProvider conversationContextProvider,
    IUserAccessTokenProvider userAccessTokenProvider,
    IAgentMessageDispatcher dispatcher)
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
        return await dispatcher.DispatchAsync(
            new AgentMessageDispatchRequest(
            request.Message,
            request.PersonId,
            request.PetId,
            request.Language,
            request.Role,
            request.IdempotencyKey,
            request.CorrelationId),
            context,
            accessToken,
            cancellationToken);
    }
}
