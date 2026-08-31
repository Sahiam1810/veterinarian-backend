using Application.Telegram.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Telegram.Workers;

public sealed class TelegramUpdateWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<TelegramUpdateWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
                    await Task.Delay(settings.WorkerPollInterval, timeProvider, stoppingToken);
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
                    "Telegram update worker cycle failed with type {ExceptionType}",
                    exception.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(2), timeProvider, stoppingToken);
            }
        }
    }
}
