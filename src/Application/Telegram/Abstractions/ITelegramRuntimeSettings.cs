namespace Application.Telegram.Abstractions;

public interface ITelegramRuntimeSettings
{
    string BotUsername { get; }

    TimeSpan LinkCodeTtl { get; }
}
