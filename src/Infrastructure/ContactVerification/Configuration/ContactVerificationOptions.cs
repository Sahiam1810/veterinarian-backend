namespace Infrastructure.ContactVerification.Configuration;

// Options propias de OTP de contacto (Etapa 3). No reutilizar AppointmentVerification.
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
