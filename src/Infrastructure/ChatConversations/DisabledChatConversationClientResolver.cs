using Application.ChatConversations.Abstraction;

namespace Infrastructure.ChatConversations;

// Ticket B7: cuando Agent__Enabled es false, IAgentConversationDefaults (de
// donde sale ClientParticipantTypeId) no está registrado — este fallback
// evita que los listados de conversaciones/escalamientos fallen al resolver
// dependencias; simplemente no enriquece con datos de cliente.
public sealed class DisabledChatConversationClientResolver : IChatConversationClientResolver
{
    public Task<ChatConversationClientInfo> ResolveAsync(
        Guid conversationId,
        CancellationToken cancellationToken) =>
        Task.FromResult(ChatConversationClientInfo.Empty);
}
