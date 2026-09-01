using System.Threading.Channels;
using Application.Telegram.Abstractions;

namespace Infrastructure.Telegram.Workers;

public sealed class InMemoryTelegramUpdateSignal : ITelegramUpdateSignal
{
    private readonly Channel<byte> channel = Channel.CreateBounded<byte>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

    public void Notify() => channel.Writer.TryWrite(0);

    public async Task WaitAsync(
        TimeSpan fallbackInterval,
        CancellationToken cancellationToken)
    {
        if (channel.Reader.TryRead(out _))
        {
            return;
        }

        using var fallback = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        fallback.CancelAfter(fallbackInterval);
        try
        {
            await channel.Reader.ReadAsync(fallback.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // El intervalo de respaldo vencio; el worker debe consultar Oracle nuevamente.
        }
    }
}
