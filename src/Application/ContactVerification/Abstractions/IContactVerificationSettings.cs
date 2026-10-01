namespace Application.ContactVerification.Abstractions;

// TTL, intentos, resend y proof de contacto (ContactVerificationOptions).
public interface IContactVerificationSettings
{
    TimeSpan OtpLifetime { get; }

    int OtpMaximumAttempts { get; }

    TimeSpan OtpResendInterval { get; }

    TimeSpan ProofLifetime { get; }
}
