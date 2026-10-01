using Domain.Common;

namespace Domain.ChatParticipants.Entities;

/// <summary>
/// Participante de una conversación de chat: un cliente o un agente humano (exactamente uno).
/// </summary>
public sealed class ChatParticipant : BaseEntity<Guid>
{
    private ChatParticipant()
    {
    }

    public Guid ChatConversationId { get; private set; }

    public Guid ParticipantTypeId { get; private set; }

    public Guid? ClientId { get; private set; }

    public Guid? AgentHumanId { get; private set; }

    public long? TelegramUserId { get; private set; }

    /// <summary>
    /// Crea un participante con exactamente una identidad válida.
    /// </summary>
    public static ChatParticipant Create(
        Guid chatConversationId,
        Guid participantTypeId,
        Guid? clientId = null,
        Guid? agentHumanId = null,
        long? telegramUserId = null)
    {
        EnsureChatConversationId(chatConversationId);
        EnsureParticipantTypeId(participantTypeId);
        EnsureExactlyOneIdentity(clientId, agentHumanId, telegramUserId);

        return new ChatParticipant
        {
            Id = Guid.NewGuid(),
            ChatConversationId = chatConversationId,
            ParticipantTypeId = participantTypeId,
            ClientId = clientId,
            AgentHumanId = agentHumanId,
            TelegramUserId = telegramUserId
        };
    }

    /// <summary>
    /// Cambia la identidad del participante manteniendo exactamente una identidad válida.
    /// </summary>
    public void ChangeIdentity(
        Guid? clientId = null,
        Guid? agentHumanId = null,
        long? telegramUserId = null)
    {
        EnsureExactlyOneIdentity(clientId, agentHumanId, telegramUserId);

        ClientId = clientId;
        AgentHumanId = agentHumanId;
        TelegramUserId = telegramUserId;
        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
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

    private static void EnsureParticipantTypeId(Guid participantTypeId)
    {
        if (participantTypeId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del tipo de participante es obligatorio.",
                nameof(participantTypeId));
        }
    }

    private static void EnsureExactlyOneIdentity(
        Guid? clientId,
        Guid? agentHumanId,
        long? telegramUserId)
    {
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del cliente no puede ser vacío.",
                nameof(clientId));
        }

        if (agentHumanId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del agente humano no puede ser vacío.",
                nameof(agentHumanId));
        }

        if (telegramUserId is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(telegramUserId),
                "El identificador de Telegram debe ser positivo.");
        }

        if ((clientId.HasValue ? 1 : 0) +
            (agentHumanId.HasValue ? 1 : 0) +
            (telegramUserId.HasValue ? 1 : 0) != 1)
        {
            throw new ArgumentException(
                "El participante debe tener exactamente una identidad.",
                nameof(clientId));
        }
    }
}
