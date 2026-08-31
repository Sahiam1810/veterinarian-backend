using Application.Telegram.Abstractions;

namespace Infrastructure.Telegram.Configuration;

public sealed record ConfiguredTelegramRuntimeSettings(
    string BotUsername,
    TimeSpan LinkCodeTtl,
    TimeSpan WorkerPollInterval,
    TimeSpan ProcessingLease,
    int MaxProcessingAttempts,
    TimeSpan DelegatedTokenLifetime,
    TimeSpan OtpLifetime,
    int OtpMaximumAttempts,
    TimeSpan OtpResendInterval) : ITelegramRuntimeSettings;
