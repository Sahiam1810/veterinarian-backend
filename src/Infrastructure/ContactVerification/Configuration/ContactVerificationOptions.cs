namespace Infrastructure.ContactVerification.Configuration;

// Stub de opciones: TTL, intentos y resend. 3.1–3.2 cablean el envío real.
public sealed class ContactVerificationOptions
{
    public const string SectionName = "ContactVerification";

    public int OtpTtlMinutes { get; init; } = 10;

    public int OtpMaximumAttempts { get; init; } = 5;

    public int OtpResendSeconds { get; init; } = 60;

    public int ProofTtlMinutes { get; init; } = 15;

    // Pepper propio o vacío para reutilizar Appointment/Telegram.
    public string? OtpPepperBase64 { get; init; }
}
