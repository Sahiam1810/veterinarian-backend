namespace Infrastructure.Email.Configuration;

// Opciones de configuración de verificación por correo electrónico (TTL, intentos, resend).
public sealed class EmailVerificationOptions
{
    public const string SectionName = "EmailVerification";

    public int OtpTtlMinutes { get; init; } = 5;

    public int OtpMaximumAttempts { get; init; } = 5;

    public int OtpResendSeconds { get; init; } = 60;

    public string? OtpPepperBase64 { get; init; }
}
