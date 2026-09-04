using Domain.Common;
using Domain.Telegram.Enums;

namespace Domain.Telegram.Entities;

public sealed class TelegramIdentitySession : BaseEntity<Guid>
{
    private const int Sha256HexLength = 64;

    private TelegramIdentitySession()
    {
    }

    public long TelegramUserId { get; private set; }
    public long TelegramChatId { get; private set; }
    public Guid? PersonId { get; private set; }
    public string? ProtectedIdentification { get; private set; }
    public string? ProtectedFullName { get; private set; }
    public string? ProtectedEmail { get; private set; }
    public string? OtpHash { get; private set; }
    public TelegramIdentitySessionStatus Status { get; private set; }
    public int OtpAttempts { get; private set; }
    public DateTime? OtpExpiresAt { get; private set; }
    public DateTime? AbsoluteExpiresAt { get; private set; }
    public DateTime? IdleExpiresAt { get; private set; }
    public long? PendingInboundUpdateId { get; private set; }

    public static TelegramIdentitySession Start(
        long telegramUserId,
        long telegramChatId,
        long pendingInboundUpdateId,
        DateTime now)
    {
        EnsureExternalId(telegramUserId, nameof(telegramUserId));
        EnsureExternalId(telegramChatId, nameof(telegramChatId));
        EnsureExternalId(pendingInboundUpdateId, nameof(pendingInboundUpdateId));

        return new TelegramIdentitySession
        {
            Id = Guid.NewGuid(),
            TelegramUserId = telegramUserId,
            TelegramChatId = telegramChatId,
            PendingInboundUpdateId = pendingInboundUpdateId,
            Status = TelegramIdentitySessionStatus.AwaitingIdentification,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void BeginKnownClientOtp(
        Guid personId,
        string otpHash,
        DateTime otpExpiresAt,
        DateTime now)
    {
        EnsureStatus(TelegramIdentitySessionStatus.AwaitingIdentification);
        EnsurePersonId(personId);
        BeginOtp(otpHash, otpExpiresAt, now);
        PersonId = personId;
    }

    public void RequireRegistration(string protectedIdentification, DateTime now)
    {
        EnsureStatus(TelegramIdentitySessionStatus.AwaitingIdentification);
        ProtectedIdentification = EnsureProtectedValue(
            protectedIdentification,
            nameof(protectedIdentification));
        Status = TelegramIdentitySessionStatus.AwaitingRegistrationConfirmation;
        UpdatedAt = now;
    }

    public void ConfirmRegistration(DateTime now)
    {
        EnsureStatus(TelegramIdentitySessionStatus.AwaitingRegistrationConfirmation);
        Status = TelegramIdentitySessionStatus.AwaitingFullName;
        UpdatedAt = now;
    }

    public void CaptureFullName(string protectedFullName, DateTime now)
    {
        EnsureStatus(TelegramIdentitySessionStatus.AwaitingFullName);
        ProtectedFullName = EnsureProtectedValue(protectedFullName, nameof(protectedFullName));
        Status = TelegramIdentitySessionStatus.AwaitingEmail;
        UpdatedAt = now;
    }

    public void BeginRegistrationOtp(
        string protectedEmail,
        string otpHash,
        DateTime otpExpiresAt,
        DateTime now)
    {
        EnsureStatus(TelegramIdentitySessionStatus.AwaitingEmail);
        ProtectedEmail = EnsureProtectedValue(protectedEmail, nameof(protectedEmail));
        BeginOtp(otpHash, otpExpiresAt, now);
    }

    public void RegisterFailedOtpAttempt(int maximumAttempts, DateTime now)
    {
        EnsureOtpActive(now);
        if (maximumAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts));
        }

        OtpAttempts++;
        if (OtpAttempts >= maximumAttempts)
        {
            Status = TelegramIdentitySessionStatus.Blocked;
            OtpHash = null;
            OtpExpiresAt = null;
        }

        UpdatedAt = now;
    }

    public void Verify(
        Guid personId,
        DateTime absoluteExpiresAt,
        DateTime idleExpiresAt,
        DateTime now)
    {
        EnsureOtpActive(now);
        EnsurePersonId(personId);
        if (absoluteExpiresAt <= now)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteExpiresAt));
        }

        if (idleExpiresAt <= now)
        {
            throw new ArgumentOutOfRangeException(nameof(idleExpiresAt));
        }

        PersonId = personId;
        AbsoluteExpiresAt = absoluteExpiresAt;
        IdleExpiresAt = idleExpiresAt <= absoluteExpiresAt ? idleExpiresAt : absoluteExpiresAt;
        Status = TelegramIdentitySessionStatus.Verified;
        OtpHash = null;
        OtpExpiresAt = null;
        OtpAttempts = 0;
        ProtectedIdentification = null;
        ProtectedFullName = null;
        ProtectedEmail = null;
        UpdatedAt = now;
    }

    public bool IsAccessValid(DateTime now) =>
        Status == TelegramIdentitySessionStatus.Verified &&
        AbsoluteExpiresAt is not null &&
        IdleExpiresAt is not null &&
        now < AbsoluteExpiresAt &&
        now < IdleExpiresAt;

    public void Touch(DateTime idleExpiresAt, DateTime now)
    {
        if (!IsAccessValid(now))
        {
            throw new InvalidOperationException("La sesión de acceso no está vigente.");
        }

        if (idleExpiresAt <= now)
        {
            throw new ArgumentOutOfRangeException(nameof(idleExpiresAt));
        }

        IdleExpiresAt = idleExpiresAt <= AbsoluteExpiresAt
            ? idleExpiresAt
            : AbsoluteExpiresAt;
        UpdatedAt = now;
    }

    public long? TakePendingInboundUpdate(DateTime now)
    {
        if (Status != TelegramIdentitySessionStatus.Verified)
        {
            throw new InvalidOperationException("La identidad todavía no está verificada.");
        }

        var pending = PendingInboundUpdateId;
        if (pending is not null)
        {
            PendingInboundUpdateId = null;
            UpdatedAt = now;
        }

        return pending;
    }

    public void Cancel(DateTime now)
    {
        EnsureNotTerminal();
        Status = TelegramIdentitySessionStatus.Cancelled;
        ClearSensitiveState();
        PendingInboundUpdateId = null;
        UpdatedAt = now;
    }

    public void Expire(DateTime now)
    {
        EnsureNotTerminal();
        Status = TelegramIdentitySessionStatus.Expired;
        ClearSensitiveState();
        PendingInboundUpdateId = null;
        UpdatedAt = now;
    }

    private void BeginOtp(string otpHash, DateTime otpExpiresAt, DateTime now)
    {
        EnsureHash(otpHash, nameof(otpHash));
        if (otpExpiresAt <= now)
        {
            throw new ArgumentOutOfRangeException(nameof(otpExpiresAt));
        }

        OtpHash = otpHash;
        OtpExpiresAt = otpExpiresAt;
        OtpAttempts = 0;
        Status = TelegramIdentitySessionStatus.AwaitingOtp;
        UpdatedAt = now;
    }

    private void EnsureOtpActive(DateTime now)
    {
        if (Status != TelegramIdentitySessionStatus.AwaitingOtp ||
            OtpExpiresAt is null ||
            now >= OtpExpiresAt)
        {
            throw new InvalidOperationException("La verificación OTP no está activa.");
        }
    }

    private void EnsureNotTerminal()
    {
        if (Status is TelegramIdentitySessionStatus.Cancelled or
            TelegramIdentitySessionStatus.Expired or
            TelegramIdentitySessionStatus.Blocked)
        {
            throw new InvalidOperationException("La sesión ya terminó.");
        }
    }

    private void EnsureStatus(TelegramIdentitySessionStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException("La transición de sesión no es válida.");
        }
    }

    private void ClearSensitiveState()
    {
        OtpHash = null;
        OtpExpiresAt = null;
        ProtectedIdentification = null;
        ProtectedFullName = null;
        ProtectedEmail = null;
    }

    private static void EnsureExternalId(long value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void EnsurePersonId(Guid personId)
    {
        if (personId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la persona no puede estar vacío.",
                nameof(personId));
        }
    }

    private static string EnsureProtectedValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("El valor protegido es obligatorio.", parameterName);
        }

        return value;
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
