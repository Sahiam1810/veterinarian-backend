using Application.Telegram.Abstractions;

namespace Infrastructure.Telegram.Configuration;

public sealed record ConfiguredTelegramRuntimeSettings(
    string BotUsername,
    TimeSpan LinkCodeTtl,
    TimeSpan WorkerPollInterval,
    int MaxProcessingAttempts,
    TimeSpan DelegatedTokenLifetime) : ITelegramRuntimeSettings;
