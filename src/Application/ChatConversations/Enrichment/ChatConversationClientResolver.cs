using Application.Agent.Abstractions;
using Application.ChatConversations.Abstraction;
using Application.Common.Abstractions;

namespace Application.ChatConversations.Enrichment;

public sealed class ChatConversationClientResolver(
    IUnitOfWork uow,
    IAgentConversationDefaults conversationDefaults) : IChatConversationClientResolver
{
    public async Task<ChatConversationClientInfo> ResolveAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var participants = await uow.ChatParticipantsRepository.GetAllByConversationIdAsync(
            conversationId, cancellationToken);
        var clientParticipant = participants.FirstOrDefault(
            participant => participant.ParticipantTypeId == conversationDefaults.ClientParticipantTypeId);
        if (clientParticipant?.ClientId is not { } clientId)
        {
            return ChatConversationClientInfo.Empty;
        }

        var client = await uow.ClientsRepository.GetByIdAsync(clientId, cancellationToken);
        if (client is null)
        {
            return ChatConversationClientInfo.Empty;
        }

        return new ChatConversationClientInfo(client.Id, client.FullName.Value, client.PhoneNumber?.Value);

    }
}
