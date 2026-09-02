namespace Application.Verification.Abstractions;

// Ajustes de OTP para acciones de cita (TTL, intentos, reenvío).
public interface IAppointmentVerificationSettings
{
    TimeSpan OtpLifetime { get; }

    int OtpMaximumAttempts { get; }

    TimeSpan OtpResendInterval { get; }
}
