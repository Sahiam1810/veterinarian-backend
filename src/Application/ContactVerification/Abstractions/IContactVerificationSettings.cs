namespace Application.ContactVerification.Abstractions;

// TTL, intentos y resend de OTP/proof de contacto (stub de configuración).
public interface IContactVerificationSettings
{
    TimeSpan OtpLifetime { get; }

    int OtpMaximumAttempts { get; }

    TimeSpan OtpResendInterval { get; }

    TimeSpan ProofLifetime { get; }
}
