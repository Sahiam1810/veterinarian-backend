using Domain.Common;

namespace Domain.Telegram.Entities;

public sealed class TelegramUserLink : BaseEntity<Guid>
{
    private TelegramUserLink()
    {
    }

    public Guid ClientId { get; private set; }

    public long TelegramUserId { get; private set; }

    public long TelegramChatId { get; private set; }

    public Guid? ChatConversationId { get; private set; }

    public DateTime LinkedAt { get; private set; }

    public DateTime? UnlinkedAt { get; private set; }

    public bool IsActive => UnlinkedAt is null;

    public void BindConversation(Guid conversationId)
    {
        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de la conversación no puede ser vacío.", nameof(conversationId));
        }

        ChatConversationId = conversationId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnbindConversation()
    {
        ChatConversationId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public static TelegramUserLink Create(
        Guid clientId,
        long telegramUserId,
        long telegramChatId,
        DateTime linkedAt)
    {
        EnsureClientId(clientId);
        EnsureExternalId(telegramUserId, nameof(telegramUserId));
        EnsureExternalId(telegramChatId, nameof(telegramChatId));

        return new TelegramUserLink
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            TelegramUserId = telegramUserId,
            TelegramChatId = telegramChatId,
            LinkedAt = linkedAt,
            CreatedAt = linkedAt,
            UpdatedAt = linkedAt
        };
    }

    public void Relink(
        long telegramUserId,
        long telegramChatId,
        DateTime linkedAt)
    {
        EnsureExternalId(telegramUserId, nameof(telegramUserId));
        EnsureExternalId(telegramChatId, nameof(telegramChatId));

        TelegramUserId = telegramUserId;
        TelegramChatId = telegramChatId;
        LinkedAt = linkedAt;
        UnlinkedAt = null;
        UpdatedAt = linkedAt;
    }

    public void Revoke(DateTime unlinkedAt)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException(
                "La vinculación de Telegram ya está revocada.");
        }

        UnlinkedAt = unlinkedAt;
        UpdatedAt = unlinkedAt;
    }

    private static void EnsureClientId(Guid clientId)
    {
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del cliente es obligatorio.",
                nameof(clientId));
        }
    }

    private static void EnsureExternalId(long value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "El identificador externo debe ser positivo.");
        }
    }
}
