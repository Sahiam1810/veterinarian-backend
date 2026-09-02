using Domain.Common;
using Domain.Verification.Enums;

namespace Domain.Verification.Entities;

// Sesión OTP genérica asociada a una acción de cita (cancelar/reagendar).
public sealed class AppointmentActionVerificationSession : BaseEntity<Guid>
{
    private const int Sha256HexLength = 64;

    private AppointmentActionVerificationSession()
    {
    }

    public Guid AppointmentId { get; private set; }

    public AppointmentVerificationAction Action { get; private set; }

    public VerificationDeliveryChannel Channel { get; private set; }

    // Hash del teléfono destino (no se guarda el número en claro).
    public string DestinationHash { get; private set; } = null!;

    public string? OtpHash { get; private set; }

    public VerificationSessionStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTime? ExpiresAt { get; private set; }

    // Payload opcional (p. ej. nueva franja al reagendar), JSON.
    public string? ActionPayload { get; private set; }

    public static AppointmentActionVerificationSession Start(
        Guid appointmentId,
        AppointmentVerificationAction action,
        VerificationDeliveryChannel channel,
        string destinationHash,
        string otpHash,
        DateTime expiresAt,
        DateTime createdAt,
        string? actionPayload = null)
    {
        if (appointmentId == Guid.Empty)
        {
            throw new ArgumentException("La cita es obligatoria.", nameof(appointmentId));
        }

        EnsureHash(destinationHash, nameof(destinationHash));
        EnsureHash(otpHash, nameof(otpHash));
        if (expiresAt <= createdAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt),
                "La expiración debe ser posterior a la creación.");
        }

        return new AppointmentActionVerificationSession
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            Action = action,
            Channel = channel,
            DestinationHash = destinationHash,
            OtpHash = otpHash,
            Status = VerificationSessionStatus.AwaitingOtp,
            Attempts = 0,
            ExpiresAt = expiresAt,
            ActionPayload = actionPayload,
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

    public void Complete(DateTime completedAt)
    {
        EnsureOtpIsActive(completedAt);
        Status = VerificationSessionStatus.Completed;
        OtpHash = null;
        UpdatedAt = completedAt;
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

        if (ExpiresAt is not null && expiredAt < ExpiresAt)
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
        if (Status != VerificationSessionStatus.AwaitingOtp ||
            ExpiresAt is null ||
            instant >= ExpiresAt)
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
