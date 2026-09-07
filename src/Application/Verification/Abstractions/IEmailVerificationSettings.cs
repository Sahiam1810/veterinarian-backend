namespace Application.Verification.Abstractions;

// Ajustes de OTP para verificación por correo electrónico (TTL, intentos, reenvío).
public interface IEmailVerificationSettings
{
    TimeSpan OtpLifetime { get; }

    int OtpMaximumAttempts { get; }

    TimeSpan OtpResendInterval { get; }
}
