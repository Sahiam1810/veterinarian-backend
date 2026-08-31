using Domain.Common;

namespace Domain.ChatMessages.Entities;

/// <summary>
/// Mensaje inmutable de una conversación de chat.
/// </summary>
public sealed class ChatMessage : BaseEntity<Guid>
{
    private ChatMessage()
    {
    }

    public Guid ChatConversationId { get; private set; }

    public Guid SenderTypesId { get; private set; }

    public Guid MessageTypeId { get; private set; }

    public Guid ChatParticipantId { get; private set; }

    public string Content { get; private set; } = null!;

    public string? Metadata { get; private set; }

    /// <summary>
    /// Crea un mensaje de chat con contenido obligatorio y metadatos opcionales.
    /// </summary>
    public static ChatMessage Create(
        Guid chatConversationId,
        Guid senderTypesId,
        Guid messageTypeId,
        Guid chatParticipantId,
        string content,
        string? metadata = null)
    {
        EnsureChatConversationId(chatConversationId);
        EnsureSenderTypesId(senderTypesId);
        EnsureMessageTypeId(messageTypeId);
        EnsureChatParticipantId(chatParticipantId);
        EnsureContent(content);

        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatConversationId = chatConversationId,
            SenderTypesId = senderTypesId,
            MessageTypeId = messageTypeId,
            ChatParticipantId = chatParticipantId,
            Content = content,
            Metadata = metadata
        };
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

    private static void EnsureSenderTypesId(Guid senderTypesId)
    {
        if (senderTypesId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del tipo de remitente es obligatorio.",
                nameof(senderTypesId));
        }
    }

    private static void EnsureMessageTypeId(Guid messageTypeId)
    {
        if (messageTypeId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del tipo de mensaje es obligatorio.",
                nameof(messageTypeId));
        }
    }

    private static void EnsureChatParticipantId(Guid chatParticipantId)
    {
        if (chatParticipantId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del participante es obligatorio.",
                nameof(chatParticipantId));
        }
    }

    private static void EnsureContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "El contenido del mensaje es obligatorio.",
                nameof(content));
        }
    }
}
