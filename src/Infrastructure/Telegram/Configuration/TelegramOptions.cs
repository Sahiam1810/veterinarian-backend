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
    public int WorkerPollMilliseconds { get; init; } = 1000;
    public int WorkerConcurrency { get; init; } = 1;
    public int ProcessingLeaseSeconds { get; init; } = 300;
    public int MaxProcessingAttempts { get; init; } = 3;
    public int DelegatedTokenMinutes { get; init; } = 5;
    public string PendingEscalationStatusId { get; init; } = string.Empty;
    public string HumanAgentSenderTypeId { get; init; } = string.Empty;
    // Pepper de respaldo para IOtpProtector (ContactVerification / citas).
    public string OtpPepperBase64 { get; init; } = string.Empty;
    public int PrivateAccessAbsoluteTtlHours { get; init; } = 24;
    public int PrivateAccessIdleTtlMinutes { get; init; } = 30;
}
