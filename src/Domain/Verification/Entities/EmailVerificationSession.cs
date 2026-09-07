using Domain.Common;
using Domain.Verification.Enums;

namespace Domain.Verification.Entities;

// Sesión OTP de verificación de correo electrónico.
public sealed class EmailVerificationSession : BaseEntity<Guid>
{
    private const int Sha256HexLength = 64;

    private EmailVerificationSession()
    {
    }

    public string Email { get; private set; } = null!;

    public string DestinationHash { get; private set; } = null!;

    public string? OtpHash { get; private set; }

    public VerificationSessionStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public string? ProofToken { get; private set; }

    public DateTime? VerifiedAt { get; private set; }

    public static EmailVerificationSession Start(
        string email,
        string destinationHash,
        string otpHash,
        DateTime expiresAt,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("El correo es obligatorio.", nameof(email));
        }

        EnsureHash(destinationHash, nameof(destinationHash));
        EnsureHash(otpHash, nameof(otpHash));

        if (expiresAt <= createdAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt),
                "La expiración debe ser posterior a la creación.");
        }

        return new EmailVerificationSession
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            DestinationHash = destinationHash,
            OtpHash = otpHash,
            Status = VerificationSessionStatus.AwaitingOtp,
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
            Status = VerificationSessionStatus.Blocked;
            OtpHash = null;
        }

        UpdatedAt = attemptedAt;
    }

    public string Complete(DateTime completedAt)
    {
        EnsureOtpIsActive(completedAt);
        Status = VerificationSessionStatus.Completed;
        OtpHash = null;
        VerifiedAt = completedAt;
        ProofToken = Guid.NewGuid().ToString("N");
        UpdatedAt = completedAt;
        return ProofToken;
    }

    public void Cancel(DateTime cancelledAt)
    {
        if (Status != VerificationSessionStatus.AwaitingOtp)
        {
            throw new InvalidOperationException("Solo una sesión activa puede cancelarse.");
        }

        Status = VerificationSessionStatus.Cancelled;
        OtpHash = null;
        UpdatedAt = cancelledAt;
    }

    public void Expire(DateTime expiredAt)
    {
        if (Status != VerificationSessionStatus.AwaitingOtp)
        {
            throw new InvalidOperationException("Solo una sesión activa puede vencer.");
        }

        if (expiredAt < ExpiresAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiredAt),
                "La sesión todavía no ha vencido.");
        }

        Status = VerificationSessionStatus.Expired;
        OtpHash = null;
        UpdatedAt = expiredAt;
    }

    private void EnsureOtpIsActive(DateTime instant)
    {
        if (Status != VerificationSessionStatus.AwaitingOtp || instant >= ExpiresAt)
        {
            throw new InvalidOperationException("La verificación OTP no está activa.");
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
