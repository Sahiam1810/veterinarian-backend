using Infrastructure.Telegram.Workers;
using Xunit;

namespace Infrastructure.Tests.Telegram;

public sealed class InMemoryTelegramUpdateSignalTests
{
    [Fact]
    public async Task Notification_releases_a_waiter_without_waiting_for_the_fallback_interval()
    {
        var signal = new InMemoryTelegramUpdateSignal();
        signal.Notify();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        await signal.WaitAsync(TimeSpan.FromMinutes(1), cancellation.Token);

        Assert.False(cancellation.IsCancellationRequested);
    }
}
