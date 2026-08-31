namespace Application.Telegram.Abstractions;

public interface ITelegramRuntimeSettings
{
    string BotUsername { get; }

    TimeSpan LinkCodeTtl { get; }

    TimeSpan WorkerPollInterval { get; }

    TimeSpan ProcessingLease { get; }

    int MaxProcessingAttempts { get; }

    TimeSpan DelegatedTokenLifetime { get; }
}
