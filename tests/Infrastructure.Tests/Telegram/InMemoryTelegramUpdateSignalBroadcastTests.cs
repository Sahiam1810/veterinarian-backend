using Infrastructure.Telegram.Workers;
using Xunit;

namespace Infrastructure.Tests.Telegram;

public sealed class InMemoryTelegramUpdateSignalBroadcastTests
{
    [Fact]
    public async Task Notify_releases_all_concurrent_waiters()
    {
        var signal = new InMemoryTelegramUpdateSignal();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var waiters = Enumerable.Range(0, 4)
            .Select(_ => signal.WaitAsync(TimeSpan.FromMinutes(1), cancellation.Token))
            .ToArray();

        await Task.Delay(50);
        signal.Notify();

        await Task.WhenAll(waiters);
        Assert.False(cancellation.IsCancellationRequested);
    }
}
