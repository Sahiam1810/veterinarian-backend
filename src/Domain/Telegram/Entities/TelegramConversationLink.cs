using Domain.Common;

namespace Domain.Telegram.Entities;

public sealed class TelegramConversationLink : BaseEntity<Guid>
{
    private TelegramConversationLink()
    {
    }

    public Guid TelegramUserLinkId { get; private set; }

    public Guid ConversationId { get; private set; }

    public static TelegramConversationLink Create(
        Guid telegramUserLinkId,
        Guid conversationId,
        DateTime createdAt)
    {
        EnsureIdentifier(telegramUserLinkId, nameof(telegramUserLinkId));
        EnsureIdentifier(conversationId, nameof(conversationId));

        return new TelegramConversationLink
        {
            Id = Guid.NewGuid(),
            TelegramUserLinkId = telegramUserLinkId,
            ConversationId = conversationId,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void BindConversation(Guid conversationId, DateTime updatedAt)
    {
        EnsureIdentifier(conversationId, nameof(conversationId));
        ConversationId = conversationId;
        UpdatedAt = updatedAt;
    }

    private static void EnsureIdentifier(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador es obligatorio.",
                parameterName);
        }
    }
}
