using Domain.Common;

namespace Domain.ChatAttachments.Entities;

/// <summary>
/// Adjunto inmutable asociado a un mensaje de chat.
/// </summary>
public sealed class ChatAttachment : BaseEntity<Guid>
{
    private ChatAttachment()
    {
    }

    public Guid ChatMessageId { get; private set; }

    public string FileUrl { get; private set; } = null!;

    public string FileType { get; private set; } = null!;

    public string FileName { get; private set; } = null!;

    /// <summary>
    /// Crea un adjunto de chat con URL, tipo y nombre de archivo obligatorios.
    /// </summary>
    public static ChatAttachment Create(
        Guid chatMessageId,
        string fileUrl,
        string fileType,
        string fileName)
    {
        EnsureChatMessageId(chatMessageId);
        EnsureFileUrl(fileUrl);
        EnsureFileType(fileType);
        EnsureFileName(fileName);

        return new ChatAttachment
        {
            Id = Guid.NewGuid(),
            ChatMessageId = chatMessageId,
            FileUrl = fileUrl,
            FileType = fileType,
            FileName = fileName
        };
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

    private static void EnsureFileUrl(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            throw new ArgumentException(
                "La URL del archivo es obligatoria.",
                nameof(fileUrl));
        }
    }

    private static void EnsureFileType(string fileType)
    {
        if (string.IsNullOrWhiteSpace(fileType))
        {
            throw new ArgumentException(
                "El tipo de archivo es obligatorio.",
                nameof(fileType));
        }
    }

    private static void EnsureFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "El nombre del archivo es obligatorio.",
                nameof(fileName));
        }
    }
}
