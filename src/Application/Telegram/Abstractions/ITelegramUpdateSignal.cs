namespace Application.Telegram.Abstractions;

public interface ITelegramUpdateSignal
{
    void Notify();

    Task WaitAsync(
        TimeSpan fallbackInterval,
        CancellationToken cancellationToken);
}
