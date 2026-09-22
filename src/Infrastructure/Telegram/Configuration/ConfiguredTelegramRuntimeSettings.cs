using Application.Telegram.Abstractions;

namespace Infrastructure.Telegram.Configuration;

public sealed record ConfiguredTelegramRuntimeSettings(
    bool GuestModeEnabled,
    string BotUsername,
    TimeSpan WorkerPollInterval,
    int WorkerConcurrency,
    TimeSpan ProcessingLease,
    int MaxProcessingAttempts,
    TimeSpan DelegatedTokenLifetime,
    Guid PendingEscalationStatusId,
    Guid HumanAgentSenderTypeId,
    TimeSpan PrivateAccessAbsoluteLifetime,
    TimeSpan PrivateAccessIdleLifetime) : ITelegramRuntimeSettings;
