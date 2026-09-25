using System.Threading.Channels;
using Application.Telegram.Abstractions;

namespace Infrastructure.Telegram.Workers;

public sealed class InMemoryTelegramUpdateSignal : ITelegramUpdateSignal
{
    private readonly Channel<byte> channel = Channel.CreateBounded<byte>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = false
        });

    private readonly object gate = new();
    private TaskCompletionSource pulse = NewPulse();

    public void Notify()
    {
        channel.Writer.TryWrite(0);
        TaskCompletionSource completed;
        lock (gate)
        {
            completed = pulse;
            pulse = NewPulse();
        }

        completed.TrySetResult();
    }

    public async Task WaitAsync(
        TimeSpan fallbackInterval,
        CancellationToken cancellationToken)
    {
        if (channel.Reader.TryRead(out _))
        {
            return;
        }

        Task waitTask;
        lock (gate)
        {
            waitTask = pulse.Task;
        }

        if (waitTask.IsCompleted)
        {
            return;
        }

        using var fallback = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        fallback.CancelAfter(fallbackInterval);
        try
        {
            await waitTask.WaitAsync(fallback.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // El intervalo de respaldo vencio; el worker debe consultar Oracle nuevamente.
        }
    }

    private static TaskCompletionSource NewPulse() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
