namespace Application.Telegram.Abstractions;

public interface ITelegramRuntimeSettings
{
    bool GuestModeEnabled { get; }

    string BotUsername { get; }

    TimeSpan LinkCodeTtl { get; }

    TimeSpan WorkerPollInterval { get; }

    TimeSpan ProcessingLease { get; }

    int MaxProcessingAttempts { get; }

    TimeSpan DelegatedTokenLifetime { get; }

    TimeSpan OtpLifetime { get; }

    int OtpMaximumAttempts { get; }

    TimeSpan OtpResendInterval { get; }

    bool RegistrationEnabled { get; }

    string RegistrationCompletionUrl { get; }

    TimeSpan RegistrationOtpLifetime { get; }

    TimeSpan RegistrationTokenLifetime { get; }

    int RegistrationMaximumOtpAttempts { get; }

    TimeSpan RegistrationResendInterval { get; }
}
