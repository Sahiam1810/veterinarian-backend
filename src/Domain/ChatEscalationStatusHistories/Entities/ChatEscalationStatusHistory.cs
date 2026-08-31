using Domain.Common;

namespace Domain.ChatEscalationStatusHistories.Entities;

// Historial de cambios de estado de un escalamiento de chat.
public sealed class ChatEscalationStatusHistory : BaseEntity<Guid>
{
    private ChatEscalationStatusHistory()
    {
    }

    public Guid EscalationStatusId { get; private set; }

    public Guid ChatEscalationId { get; private set; }

    public static ChatEscalationStatusHistory Create(
        Guid escalationStatusId,
        Guid chatEscalationId)
    {
        EnsureEscalationStatusId(escalationStatusId);
        EnsureChatEscalationId(chatEscalationId);

        return new ChatEscalationStatusHistory
        {
            Id = Guid.NewGuid(),
            EscalationStatusId = escalationStatusId,
            ChatEscalationId = chatEscalationId
        };
    }

    public void Update(Guid escalationStatusId)
    {
        EnsureEscalationStatusId(escalationStatusId);

        EscalationStatusId = escalationStatusId;
        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
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

    private static void EnsureChatEscalationId(Guid chatEscalationId)
    {
        if (chatEscalationId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del escalamiento es obligatorio.",
                nameof(chatEscalationId));
        }
    }
}
