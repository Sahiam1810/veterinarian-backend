namespace Infrastructure.Telegram.Configuration;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public bool Enabled { get; init; }
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
}
