namespace Infrastructure.Verification.Configuration;

// Opciones OTP para acciones de cita (autoservicio).
public sealed class AppointmentVerificationOptions
{
    public const string SectionName = "AppointmentVerification";

    public int OtpTtlMinutes { get; init; } = 5;

    public int OtpMaximumAttempts { get; init; } = 5;

    public int OtpResendSeconds { get; init; } = 60;

    // Pepper propio o vacío para reutilizar Telegram:OtpPepperBase64.
    public string? OtpPepperBase64 { get; init; }
}
