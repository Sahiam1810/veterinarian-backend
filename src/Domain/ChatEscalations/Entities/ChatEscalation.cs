namespace Domain.ChatEscalations.Entities;

// Escalamiento de una conversación de chat.
public sealed class ChatEscalation
{
    private ChatEscalation()
    {
    }

    public Guid Id { get; private set; }

    public Guid ChatConversationId { get; private set; }

    public Guid EscalationStatusId { get; private set; }

    public bool FromAi { get; private set; }

    public string? Reason { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // Campo literal VARCHAR2(36) según el diagrama Oracle.
    public string? UpdateAt { get; private set; }

    public static ChatEscalation Create(
        Guid chatConversationId,
        Guid escalationStatusId,
        bool fromAi,
        string? reason = null,
        string? updateAt = null)
    {
        EnsureChatConversationId(chatConversationId);
        EnsureEscalationStatusId(escalationStatusId);

        return new ChatEscalation
        {
            Id = Guid.NewGuid(),
            ChatConversationId = chatConversationId,
            EscalationStatusId = escalationStatusId,
            FromAi = fromAi,
            Reason = reason,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = updateAt
        };
    }

    public void Update(
        Guid escalationStatusId,
        bool fromAi,
        string? reason,
        string? updateAt)
    {
        EnsureEscalationStatusId(escalationStatusId);

        EscalationStatusId = escalationStatusId;
        FromAi = fromAi;
        Reason = reason;
        UpdateAt = updateAt;
    }

    private static void EnsureChatConversationId(Guid chatConversationId)
    {
        if (chatConversationId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la conversación es obligatorio.",
                nameof(chatConversationId));
        }
    }

    private static void EnsureEscalationStatusId(Guid escalationStatusId)
    {
        if (escalationStatusId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del estado de escalamiento es obligatorio.",
                nameof(escalationStatusId));
        }
    }
}
