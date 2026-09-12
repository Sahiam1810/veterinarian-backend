using Application.Telegram.Abstractions;

namespace Infrastructure.Telegram.Configuration;

public sealed record ConfiguredTelegramRuntimeSettings(
    bool GuestModeEnabled,
    string BotUsername,
    TimeSpan LinkCodeTtl,
    TimeSpan WorkerPollInterval,
    TimeSpan ProcessingLease,
    int MaxProcessingAttempts,
    TimeSpan DelegatedTokenLifetime,
    TimeSpan OtpLifetime,
    int OtpMaximumAttempts,
    TimeSpan OtpResendInterval,
    TimeSpan PrivateAccessAbsoluteLifetime,
    TimeSpan PrivateAccessIdleLifetime,
    bool RegistrationEnabled,
    string RegistrationCompletionUrl,
    TimeSpan RegistrationOtpLifetime,
    TimeSpan RegistrationTokenLifetime,
    int RegistrationMaximumOtpAttempts,
    TimeSpan RegistrationResendInterval) : ITelegramRuntimeSettings;
