using Domain.Common;
using Domain.Telegram.Enums;

namespace Domain.Telegram.Entities;

public sealed class TelegramLinkingSession : BaseEntity<Guid>
{
    private const int Sha256HexLength = 64;

    private TelegramLinkingSession()
    {
    }

    public long TelegramUserId { get; private set; }

    public long TelegramChatId { get; private set; }

    public Guid? PersonId { get; private set; }

    public string? EmailHash { get; private set; }

    public string? OtpHash { get; private set; }

    public TelegramLinkingSessionStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTime? ExpiresAt { get; private set; }

    public static TelegramLinkingSession Start(
        long telegramUserId,
        long telegramChatId,
        DateTime createdAt)
    {
        EnsureExternalId(telegramUserId, nameof(telegramUserId));
        EnsureExternalId(telegramChatId, nameof(telegramChatId));

        return new TelegramLinkingSession
        {
            Id = Guid.NewGuid(),
            TelegramUserId = telegramUserId,
            TelegramChatId = telegramChatId,
            Status = TelegramLinkingSessionStatus.AwaitingEmail,
            Attempts = 0,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void ResolveAccount(
        Guid? personId,
        string emailHash,
        string otpHash,
        DateTime expiresAt,
        DateTime resolvedAt)
    {
        if (Status != TelegramLinkingSessionStatus.AwaitingEmail)
        {
            throw new InvalidOperationException(
                "La sesión no está esperando un correo.");
        }

        if (personId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la persona no puede estar vacío.",
                nameof(personId));
        }

        EnsureHash(emailHash, nameof(emailHash));
        EnsureHash(otpHash, nameof(otpHash));
        if (expiresAt <= resolvedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt),
                "La expiración debe ser posterior a la verificación.");
        }

        PersonId = personId;
        EmailHash = emailHash;
        OtpHash = otpHash;
        ExpiresAt = expiresAt;
        Attempts = 0;
        Status = TelegramLinkingSessionStatus.AwaitingOtp;
        UpdatedAt = resolvedAt;
    }

    public void RegisterFailedAttempt(int maximumAttempts, DateTime attemptedAt)
    {
        EnsureOtpIsActive(attemptedAt);
        if (maximumAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts));
        }

        Attempts++;
        if (Attempts >= maximumAttempts)
        {
            Status = TelegramLinkingSessionStatus.Blocked;
            OtpHash = null;
        }

        UpdatedAt = attemptedAt;
    }

    public void Complete(DateTime completedAt)
    {
        EnsureOtpIsActive(completedAt);
        if (PersonId is null)
        {
            throw new InvalidOperationException(
                "La sesión no corresponde a una cuenta vinculable.");
        }

        Status = TelegramLinkingSessionStatus.Linked;
        OtpHash = null;
        UpdatedAt = completedAt;
    }

    public void Cancel(DateTime cancelledAt)
    {
        if (Status is not TelegramLinkingSessionStatus.AwaitingEmail and
            not TelegramLinkingSessionStatus.AwaitingOtp)
        {
            throw new InvalidOperationException(
                "Solo una sesión activa puede cancelarse.");
        }

        Status = TelegramLinkingSessionStatus.Cancelled;
        OtpHash = null;
        UpdatedAt = cancelledAt;
    }

    public void Expire(DateTime expiredAt)
    {
        if (Status is not TelegramLinkingSessionStatus.AwaitingEmail and
            not TelegramLinkingSessionStatus.AwaitingOtp)
        {
            throw new InvalidOperationException(
                "Solo una sesión activa puede vencer.");
        }

        if (ExpiresAt is not null && expiredAt < ExpiresAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiredAt),
                "La sesión todavía no ha vencido.");
        }

        Status = TelegramLinkingSessionStatus.Expired;
        OtpHash = null;
        UpdatedAt = expiredAt;
    }

    private void EnsureOtpIsActive(DateTime instant)
    {
        if (Status != TelegramLinkingSessionStatus.AwaitingOtp ||
            ExpiresAt is null ||
            instant >= ExpiresAt)
        {
            throw new InvalidOperationException(
                "La verificación OTP no está activa.");
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

    private static void EnsureHash(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != Sha256HexLength)
        {
            throw new ArgumentException(
                "El hash debe ser SHA-256 hexadecimal.",
                parameterName);
        }
    }
}
