namespace Infrastructure.Telegram.Configuration;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public bool Enabled { get; init; }
    public bool GuestModeEnabled { get; init; }
    public string BotToken { get; init; } = string.Empty;
    public string BotUsername { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
    public string PublicWebhookUrl { get; init; } = string.Empty;
    public int LinkCodeTtlMinutes { get; init; } = 10;
    public int WorkerPollMilliseconds { get; init; } = 1000;
    public int ProcessingLeaseSeconds { get; init; } = 300;
    public int MaxProcessingAttempts { get; init; } = 3;
    public int DelegatedTokenMinutes { get; init; } = 5;
    public int OtpTtlMinutes { get; init; } = 5;
    public int OtpMaximumAttempts { get; init; } = 5;
    public int OtpResendSeconds { get; init; } = 60;
    public string OtpPepperBase64 { get; init; } = string.Empty;
    public int PrivateAccessAbsoluteTtlHours { get; init; } = 24;
    public int PrivateAccessIdleTtlMinutes { get; init; } = 30;
    public bool RegistrationEnabled { get; init; }
    public string RegistrationCompletionUrl { get; init; } = string.Empty;
    public int RegistrationOtpTtlMinutes { get; init; } = 10;
    public int RegistrationTokenTtlMinutes { get; init; } = 15;
    public int RegistrationMaxOtpAttempts { get; init; } = 3;
    public int RegistrationResendSeconds { get; init; } = 60;
    public string RegistrationProtectionKeyBase64 { get; init; } = string.Empty;
}
