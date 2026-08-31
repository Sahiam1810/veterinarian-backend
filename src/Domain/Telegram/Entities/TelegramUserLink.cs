using Domain.Common;

namespace Domain.Telegram.Entities;

public sealed class TelegramUserLink : BaseEntity<Guid>
{
    private TelegramUserLink()
    {
    }

    public Guid PersonId { get; private set; }

    public long TelegramUserId { get; private set; }

    public long TelegramChatId { get; private set; }

    public DateTime LinkedAt { get; private set; }

    public DateTime? UnlinkedAt { get; private set; }

    public bool IsActive => UnlinkedAt is null;

    public static TelegramUserLink Create(
        Guid personId,
        long telegramUserId,
        long telegramChatId,
        DateTime linkedAt)
    {
        EnsurePersonId(personId);
        EnsureExternalId(telegramUserId, nameof(telegramUserId));
        EnsureExternalId(telegramChatId, nameof(telegramChatId));

        return new TelegramUserLink
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
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

    private static void EnsurePersonId(Guid personId)
    {
        if (personId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la persona es obligatorio.",
                nameof(personId));
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
