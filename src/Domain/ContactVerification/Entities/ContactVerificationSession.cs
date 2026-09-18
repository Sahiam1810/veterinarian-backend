using Domain.Common;
using Domain.ContactVerification.Enums;

namespace Domain.ContactVerification.Entities;

// Sesión OTP de contacto (Register/Claim). No reutiliza la tabla de OTP de citas.
public sealed class ContactVerificationSession : BaseEntity<Guid>
{
    private const int Sha256HexLength = 64;

    private ContactVerificationSession()
    {
    }

    public ContactVerificationPurpose Purpose { get; private set; }

    public ContactVerificationChannel Channel { get; private set; }

    // Hash del correo destino; nunca se persiste el email en claro.
    public string DestinationHash { get; private set; } = null!;

    // Usuario ya persistido en Claim; nulo en Register.
    public Guid? SubjectUserId { get; private set; }

    public string? OtpHash { get; private set; }

    public string? ProofHash { get; private set; }

    public ContactVerificationSessionStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTime? ExpiresAt { get; private set; }

    public DateTime? ProofExpiresAt { get; private set; }

    public bool IsAlive =>
        Status is ContactVerificationSessionStatus.AwaitingOtp
            or ContactVerificationSessionStatus.ProofIssued;

    public static ContactVerificationSession Start(
        ContactVerificationPurpose purpose,
        ContactVerificationChannel channel,
        string destinationHash,
        string otpHash,
        DateTime expiresAt,
        DateTime createdAt,
        Guid? subjectUserId = null)
    {
        EnsurePurpose(purpose);
        EnsureEmailChannel(channel);
        EnsureHash(destinationHash, nameof(destinationHash));
        EnsureHash(otpHash, nameof(otpHash));
        if (expiresAt <= createdAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt),
                "La expiración debe ser posterior a la creación.");
        }

        if (purpose == ContactVerificationPurpose.Claim && subjectUserId is null)
        {
            throw new ArgumentException(
                "Claim exige el usuario sujeto.",
                nameof(subjectUserId));
        }

        if (purpose == ContactVerificationPurpose.Register && subjectUserId is not null)
        {
            throw new ArgumentException(
                "Register no admite usuario sujeto todavía.",
                nameof(subjectUserId));
        }

        return new ContactVerificationSession
        {
            Id = Guid.NewGuid(),
            Purpose = purpose,
            Channel = channel,
            DestinationHash = destinationHash,
            SubjectUserId = subjectUserId,
            OtpHash = otpHash,
            Status = ContactVerificationSessionStatus.AwaitingOtp,
            Attempts = 0,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
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
            Status = ContactVerificationSessionStatus.Blocked;
            OtpHash = null;
        }

        UpdatedAt = attemptedAt;
    }

    // Confirma el OTP y deja un proof de un solo uso (solo hash).
    public void IssueProof(string proofHash, DateTime proofExpiresAt, DateTime confirmedAt)
    {
        EnsureOtpIsActive(confirmedAt);
        EnsureHash(proofHash, nameof(proofHash));
        if (proofExpiresAt <= confirmedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(proofExpiresAt),
                "El proof debe expirar después de confirmarse.");
        }

        Status = ContactVerificationSessionStatus.ProofIssued;
        OtpHash = null;
        ProofHash = proofHash;
        ProofExpiresAt = proofExpiresAt;
        UpdatedAt = confirmedAt;
    }

    public void ConsumeProof(DateTime consumedAt)
    {
        if (Status != ContactVerificationSessionStatus.ProofIssued)
        {
            throw new InvalidOperationException("No hay proof activo para consumir.");
        }

        if (ProofExpiresAt is not null && consumedAt >= ProofExpiresAt)
        {
            throw new InvalidOperationException("El proof de contacto ha vencido.");
        }

        Status = ContactVerificationSessionStatus.Consumed;
        ProofHash = null;
        UpdatedAt = consumedAt;
    }

    public void Cancel(DateTime cancelledAt)
    {
        if (!IsAlive)
        {
            throw new InvalidOperationException("Solo una sesión viva puede cancelarse.");
        }

        Status = ContactVerificationSessionStatus.Cancelled;
        OtpHash = null;
        ProofHash = null;
        UpdatedAt = cancelledAt;
    }

    public void Expire(DateTime expiredAt)
    {
        if (Status == ContactVerificationSessionStatus.AwaitingOtp)
        {
            if (ExpiresAt is not null && expiredAt < ExpiresAt)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expiredAt),
                    "La sesión todavía no ha vencido.");
            }
        }
        else if (Status == ContactVerificationSessionStatus.ProofIssued)
        {
            if (ProofExpiresAt is not null && expiredAt < ProofExpiresAt)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expiredAt),
                    "El proof todavía no ha vencido.");
            }
        }
        else
        {
            throw new InvalidOperationException("Solo una sesión viva puede vencer.");
        }

        Status = ContactVerificationSessionStatus.Expired;
        OtpHash = null;
        ProofHash = null;
        UpdatedAt = expiredAt;
    }

    private void EnsureOtpIsActive(DateTime instant)
    {
        if (Status != ContactVerificationSessionStatus.AwaitingOtp ||
            ExpiresAt is null ||
            instant >= ExpiresAt)
        {
            throw new InvalidOperationException("La verificación OTP de contacto no está activa.");
        }
    }

    private static void EnsureEmailChannel(ContactVerificationChannel channel)
    {
        if (channel != ContactVerificationChannel.Email)
        {
            throw new ArgumentOutOfRangeException(
                nameof(channel),
                "v1 solo admite Email como canal de contacto.");
        }
    }

    private static void EnsurePurpose(ContactVerificationPurpose purpose)
    {
        if (purpose is not ContactVerificationPurpose.Register
            and not ContactVerificationPurpose.Claim)
        {
            throw new ArgumentOutOfRangeException(nameof(purpose));
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
