using Domain.Common;

namespace Domain.ChatConversationAiSettings.Entities;

// Configuración de IA asociada a una conversación de chat.
public sealed class ChatConversationAiSetting : BaseEntity<Guid>
{
    private ChatConversationAiSetting()
    {
    }

    public Guid ConversationId { get; private set; }

    public bool AiEnabled { get; private set; }

    public Guid? DefaultModelId { get; private set; }

    // Crea la configuración de IA para una conversación.
    public static ChatConversationAiSetting Create(
        Guid conversationId,
        bool aiEnabled,
        Guid? defaultModelId = null)
    {
        EnsureConversationId(conversationId);
        EnsureDefaultModelId(defaultModelId);

        return new ChatConversationAiSetting
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            AiEnabled = aiEnabled,
            DefaultModelId = defaultModelId
        };
    }

    // Actualiza la configuración de IA.
    public void Update(bool aiEnabled, Guid? defaultModelId)
    {
        EnsureDefaultModelId(defaultModelId);

        AiEnabled = aiEnabled;
        DefaultModelId = defaultModelId;
        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    private static void EnsureConversationId(Guid conversationId)
    {
        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la conversación es obligatorio.",
                nameof(conversationId));
        }
    }

    private static void EnsureDefaultModelId(Guid? defaultModelId)
    {
        if (defaultModelId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del modelo por defecto no puede ser vacío.",
                nameof(defaultModelId));
        }
    }
}
