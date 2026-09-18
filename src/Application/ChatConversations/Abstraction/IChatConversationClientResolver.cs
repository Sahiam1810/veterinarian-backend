namespace Application.ChatConversations.Abstraction;

// Ticket B7: cliente vinculado a una conversación (participante Cliente →
// ChatUserProfile → Client → User), reutilizado por los listados REST de
// conversaciones/escalamientos y por el broadcast de SignalR (Ticket B5).
public sealed record ChatConversationClientInfo(
    Guid? ClientId,
    string? ClientName,
    string? ClientPhone)
{
    public static readonly ChatConversationClientInfo Empty = new(null, null, null);
}

public interface IChatConversationClientResolver
{
    Task<ChatConversationClientInfo> ResolveAsync(
        Guid conversationId,
        CancellationToken cancellationToken);
}
