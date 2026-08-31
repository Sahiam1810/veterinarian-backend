using Domain.Common;

namespace Domain.ChatParticipants.Entities;

/// <summary>
/// Participante de una conversación de chat con identidad polimórfica (perfil, agente humano o modelo IA).
/// </summary>
public sealed class ChatParticipant : BaseEntity<Guid>
{
    private ChatParticipant()
    {
    }

    public Guid ChatConversationId { get; private set; }

    public Guid ParticipantTypeId { get; private set; }

    public Guid? ChatUserProfileId { get; private set; }

    public Guid? AgentHumanId { get; private set; }

    public Guid? AiModelId { get; private set; }

    /// <summary>
    /// Crea un participante con exactamente una identidad válida.
    /// </summary>
    public static ChatParticipant Create(
        Guid chatConversationId,
        Guid participantTypeId,
        Guid? chatUserProfileId = null,
        Guid? agentHumanId = null,
        Guid? aiModelId = null)
    {
        EnsureChatConversationId(chatConversationId);
        EnsureParticipantTypeId(participantTypeId);
        EnsureExactlyOneIdentity(chatUserProfileId, agentHumanId, aiModelId);

        return new ChatParticipant
        {
            Id = Guid.NewGuid(),
            ChatConversationId = chatConversationId,
            ParticipantTypeId = participantTypeId,
            ChatUserProfileId = chatUserProfileId,
            AgentHumanId = agentHumanId,
            AiModelId = aiModelId
        };
    }

    /// <summary>
    /// Cambia la identidad del participante manteniendo exactamente una identidad válida.
    /// </summary>
    public void ChangeIdentity(
        Guid? chatUserProfileId = null,
        Guid? agentHumanId = null,
        Guid? aiModelId = null)
    {
        EnsureExactlyOneIdentity(chatUserProfileId, agentHumanId, aiModelId);

        ChatUserProfileId = chatUserProfileId;
        AgentHumanId = agentHumanId;
        AiModelId = aiModelId;
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
        Guid? chatUserProfileId,
        Guid? agentHumanId,
        Guid? aiModelId)
    {
        if (chatUserProfileId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del perfil de chat no puede ser vacío.",
                nameof(chatUserProfileId));
        }

        if (agentHumanId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del agente humano no puede ser vacío.",
                nameof(agentHumanId));
        }

        if (aiModelId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del modelo de IA no puede ser vacío.",
                nameof(aiModelId));
        }

        var identityCount = 0;

        if (chatUserProfileId.HasValue)
        {
            identityCount++;
        }

        if (agentHumanId.HasValue)
        {
            identityCount++;
        }

        if (aiModelId.HasValue)
        {
            identityCount++;
        }

        if (identityCount != 1)
        {
            throw new ArgumentException(
                "El participante debe tener exactamente una identidad (perfil de chat, agente humano o modelo de IA).",
                nameof(chatUserProfileId));
        }
    }
}
