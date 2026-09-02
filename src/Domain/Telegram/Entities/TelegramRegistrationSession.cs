using Domain.Common;
using Domain.Telegram.Enums;

namespace Domain.Telegram.Entities;

public sealed class TelegramRegistrationSession : BaseEntity<Guid>
{
    private const int Sha256HexLength = 64;

    private TelegramRegistrationSession()
    {
    }

    public long TelegramUserId { get; private set; }
    public long TelegramChatId { get; private set; }
    public Guid? PersonId { get; private set; }
    public string? ProtectedEmail { get; private set; }
    public string? EmailHash { get; private set; }
    public string? OtpHash { get; private set; }
    public string? CompletionTokenHash { get; private set; }
    public TelegramRegistrationAccountKind AccountKind { get; private set; }
    public TelegramRegistrationSessionStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTime? OtpExpiresAt { get; private set; }
    public DateTime? CompletionExpiresAt { get; private set; }

    public static TelegramRegistrationSession Start(
        long telegramUserId,
        long telegramChatId,
        DateTime createdAt)
    {
        EnsureExternalId(telegramUserId, nameof(telegramUserId));
        EnsureExternalId(telegramChatId, nameof(telegramChatId));

        return new TelegramRegistrationSession
        {
            Id = Guid.NewGuid(),
            TelegramUserId = telegramUserId,
            TelegramChatId = telegramChatId,
            AccountKind = TelegramRegistrationAccountKind.New,
            Status = TelegramRegistrationSessionStatus.AwaitingEmail,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void PrepareOtp(
        string protectedEmail,
        string emailHash,
        string otpHash,
        TelegramRegistrationAccountKind accountKind,
        Guid? personId,
        DateTime expiresAt,
        DateTime preparedAt)
    {
        if (Status != TelegramRegistrationSessionStatus.AwaitingEmail)
        {
            throw new InvalidOperationException("La sesión no está esperando un correo.");
        }

        if (string.IsNullOrWhiteSpace(protectedEmail))
        {
            throw new ArgumentException("El correo protegido es obligatorio.", nameof(protectedEmail));
        }

        EnsureHash(emailHash, nameof(emailHash));
        EnsureHash(otpHash, nameof(otpHash));
        if (expiresAt <= preparedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAt));
        }

        if (accountKind == TelegramRegistrationAccountKind.Active &&
            (!personId.HasValue || personId.Value == Guid.Empty))
        {
            throw new ArgumentException("Una cuenta activa requiere una persona.", nameof(personId));
        }

        ProtectedEmail = protectedEmail;
        EmailHash = emailHash;
        OtpHash = otpHash;
        AccountKind = accountKind;
        PersonId = personId;
        Attempts = 0;
        OtpExpiresAt = expiresAt;
        Status = TelegramRegistrationSessionStatus.AwaitingOtp;
        UpdatedAt = preparedAt;
    }

    public void RegisterFailedOtp(int maximumAttempts, DateTime attemptedAt)
    {
        EnsureOtpActive(attemptedAt);
        if (maximumAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts));
        }

        Attempts++;
        if (Attempts >= maximumAttempts)
        {
            Status = TelegramRegistrationSessionStatus.Blocked;
            OtpHash = null;
        }

        UpdatedAt = attemptedAt;
    }

    public void VerifyOtp(DateTime verifiedAt)
    {
        EnsureOtpActive(verifiedAt);
        OtpHash = null;
        Attempts = 0;
        UpdatedAt = verifiedAt;
    }

    public void IssueCompletionToken(
        string tokenHash,
        DateTime expiresAt,
        DateTime issuedAt)
    {
        if (AccountKind != TelegramRegistrationAccountKind.New ||
            Status != TelegramRegistrationSessionStatus.AwaitingOtp ||
            OtpHash is not null)
        {
            throw new InvalidOperationException("El correo aún no está verificado para registro.");
        }

        EnsureHash(tokenHash, nameof(tokenHash));
        if (expiresAt <= issuedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAt));
        }

        CompletionTokenHash = tokenHash;
        CompletionExpiresAt = expiresAt;
        Status = TelegramRegistrationSessionStatus.AwaitingProfile;
        UpdatedAt = issuedAt;
    }

    public void Complete(Guid personId, DateTime completedAt)
    {
        var verifiedActiveAccount =
            Status == TelegramRegistrationSessionStatus.AwaitingOtp &&
            AccountKind == TelegramRegistrationAccountKind.Active &&
            OtpHash is null;
        var completedProfile =
            Status == TelegramRegistrationSessionStatus.AwaitingProfile &&
            CompletionTokenHash is not null &&
            CompletionExpiresAt is not null &&
            completedAt < CompletionExpiresAt;

        if (!verifiedActiveAccount && !completedProfile)
        {
            throw new InvalidOperationException("La sesión no puede completarse.");
        }

        if (personId == Guid.Empty)
        {
            throw new ArgumentException("La persona es obligatoria.", nameof(personId));
        }

        PersonId = personId;
        OtpHash = null;
        CompletionTokenHash = null;
        Status = TelegramRegistrationSessionStatus.Completed;
        UpdatedAt = completedAt;
    }

    public void Cancel(DateTime cancelledAt)
    {
        EnsureActive();
        ClearSecrets();
        Status = TelegramRegistrationSessionStatus.Cancelled;
        UpdatedAt = cancelledAt;
    }

    public void Expire(DateTime expiredAt)
    {
        EnsureActive();
        ClearSecrets();
        Status = TelegramRegistrationSessionStatus.Expired;
        UpdatedAt = expiredAt;
    }

    private void EnsureOtpActive(DateTime instant)
    {
        if (Status != TelegramRegistrationSessionStatus.AwaitingOtp ||
            OtpHash is null ||
            OtpExpiresAt is null ||
            instant >= OtpExpiresAt)
        {
            throw new InvalidOperationException("La verificación OTP no está activa.");
        }
    }

    private void EnsureActive()
    {
        if (Status is not TelegramRegistrationSessionStatus.AwaitingEmail and
            not TelegramRegistrationSessionStatus.AwaitingOtp and
            not TelegramRegistrationSessionStatus.AwaitingProfile)
        {
            throw new InvalidOperationException("La sesión ya no está activa.");
        }
    }

    private void ClearSecrets()
    {
        OtpHash = null;
        CompletionTokenHash = null;
    }

    private static void EnsureHash(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != Sha256HexLength)
        {
            throw new ArgumentException("El hash debe ser SHA-256 hexadecimal.", parameterName);
        }
    }

    private static void EnsureExternalId(long value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
