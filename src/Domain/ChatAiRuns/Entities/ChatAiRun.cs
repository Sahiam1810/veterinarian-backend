using Domain.Common;

namespace Domain.ChatAiRuns.Entities;

/// <summary>
/// Cabecera de una ejecución de IA asociada a un mensaje de chat.
/// </summary>
public sealed class ChatAiRun : BaseEntity<Guid>
{
    private ChatAiRun()
    {
    }

    public Guid ChatConversationId { get; private set; }

    public Guid ChatMessageId { get; private set; }

    public Guid AiModelId { get; private set; }

    public Guid AiRunStatusId { get; private set; }

    /// <summary>
    /// Crea una ejecución de IA con timestamps UTC iniciales iguales.
    /// </summary>
    public static ChatAiRun Create(
        Guid chatConversationId,
        Guid chatMessageId,
        Guid aiModelId,
        Guid aiRunStatusId)
    {
        EnsureChatConversationId(chatConversationId);
        EnsureChatMessageId(chatMessageId);
        EnsureAiModelId(aiModelId);
        EnsureAiRunStatusId(aiRunStatusId);

        var now = DateTime.UtcNow;

        return new ChatAiRun
        {
            Id = Guid.NewGuid(),
            ChatConversationId = chatConversationId,
            ChatMessageId = chatMessageId,
            AiModelId = aiModelId,
            AiRunStatusId = aiRunStatusId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Actualiza únicamente el estado de ejecución y la marca temporal de modificación.
    /// </summary>
    public void UpdateStatus(Guid aiRunStatusId)
    {
        EnsureAiRunStatusId(aiRunStatusId);
        AiRunStatusId = aiRunStatusId;
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

    private static void EnsureChatMessageId(Guid chatMessageId)
    {
        if (chatMessageId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del mensaje es obligatorio.",
                nameof(chatMessageId));
        }
    }

    private static void EnsureAiModelId(Guid aiModelId)
    {
        if (aiModelId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del modelo de IA es obligatorio.",
                nameof(aiModelId));
        }
    }

    private static void EnsureAiRunStatusId(Guid aiRunStatusId)
    {
        if (aiRunStatusId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del estado de ejecución es obligatorio.",
                nameof(aiRunStatusId));
        }
    }
}
