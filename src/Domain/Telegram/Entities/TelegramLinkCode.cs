using Domain.Common;

namespace Domain.Telegram.Entities;

public sealed class TelegramLinkCode : BaseEntity<Guid>
{
    private const int Sha256HexLength = 64;

    private TelegramLinkCode()
    {
    }

    public Guid PersonId { get; private set; }

    public string CodeHash { get; private set; } = null!;

    public DateTime ExpiresAt { get; private set; }

    public DateTime? ConsumedAt { get; private set; }

    public DateTime? InvalidatedAt { get; private set; }

    public static TelegramLinkCode Create(
        Guid personId,
        string codeHash,
        DateTime expiresAt,
        DateTime createdAt)
    {
        if (personId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la persona es obligatorio.",
                nameof(personId));
        }

        if (string.IsNullOrWhiteSpace(codeHash) ||
            codeHash.Length != Sha256HexLength)
        {
            throw new ArgumentException(
                "El hash del código de vinculación debe ser SHA-256 hexadecimal.",
                nameof(codeHash));
        }

        if (expiresAt <= createdAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt),
                "La expiración debe ser posterior a la creación.");
        }

        return new TelegramLinkCode
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            CodeHash = codeHash,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public bool IsActiveAt(DateTime instant) =>
        ConsumedAt is null &&
        InvalidatedAt is null &&
        instant < ExpiresAt;

    public void Consume(DateTime consumedAt)
    {
        if (!IsActiveAt(consumedAt))
        {
            throw new InvalidOperationException(
                "El código de vinculación no está activo.");
        }

        ConsumedAt = consumedAt;
        UpdatedAt = consumedAt;
    }

    public void Invalidate(DateTime invalidatedAt)
    {
        if (ConsumedAt is not null)
        {
            throw new InvalidOperationException(
                "Un código consumido no puede invalidarse.");
        }

        if (InvalidatedAt is not null)
        {
            return;
        }

        InvalidatedAt = invalidatedAt;
        UpdatedAt = invalidatedAt;
    }
}
