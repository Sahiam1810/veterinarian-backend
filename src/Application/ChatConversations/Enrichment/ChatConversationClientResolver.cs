using Application.Agent.Abstractions;
using Application.ChatConversations.Abstraction;
using Application.Common.Abstractions;

namespace Application.ChatConversations.Enrichment;

// Ticket B7: extraído de CreateChatEscalationCommandHandler.ResolveClientAsync
// (Ticket B5) para reutilizarlo también en los listados REST de
// conversaciones y escalamientos — antes solo lo usaba el broadcast de SignalR.
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
        if (clientParticipant?.ChatUserProfileId is not { } profileId)
        {
            return ChatConversationClientInfo.Empty;
        }

        var profile = await uow.ChatUserProfilesRepository.GetByIdAsync(profileId, cancellationToken);
        if (profile is null)
        {
            return ChatConversationClientInfo.Empty;
        }

        var client = await uow.ClientsRepository.GetByUserIdAsync(profile.UserId, cancellationToken);
        if (client is null)
        {
            return ChatConversationClientInfo.Empty;
        }

        if (client.UserId is null)
        {
            return new ChatConversationClientInfo(client.Id, client.FullName.Value, client.PhoneNumber?.Value);
        }

        return new ChatConversationClientInfo(client.Id, client.FullName.Value, client.PhoneNumber?.Value);
    }
}
