using Application.Telegram.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Telegram.Workers;

public sealed class TelegramUpdateWorker(
    IServiceScopeFactory scopeFactory,
    ITelegramUpdateSignal updateSignal,
    TimeProvider timeProvider,
    ILogger<TelegramUpdateWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var bootstrapScope = scopeFactory.CreateAsyncScope();
        var settings = bootstrapScope.ServiceProvider.GetRequiredService<ITelegramRuntimeSettings>();
        var concurrency = Math.Max(1, settings.WorkerConcurrency);
        var slots = Enumerable.Range(0, concurrency)
            .Select(slot => RunSlotAsync(slot, stoppingToken))
            .ToArray();
        await Task.WhenAll(slots);
    }

    private async Task RunSlotAsync(int slot, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var pump = scope.ServiceProvider.GetRequiredService<TelegramUpdatePump>();
                var settings = scope.ServiceProvider.GetRequiredService<ITelegramRuntimeSettings>();
                var processed = await pump.RunOnceAsync(stoppingToken);
                if (!processed)
                {
                    await updateSignal.WaitAsync(settings.WorkerPollInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    new EventId(7101, "TelegramWorkerCycleFailed"),
                    exception,
                    "Telegram update worker slot {Slot} cycle failed with type {ExceptionType}",
                    slot,
                    exception.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(2), timeProvider, stoppingToken);
            }
        }
    }
}
