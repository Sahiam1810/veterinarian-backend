namespace Application.Telegram.Abstractions;

public interface ITelegramBotClient
{
    Task<long> SendTextAsync(
        long chatId,
        string text,
        CancellationToken cancellationToken);
}
