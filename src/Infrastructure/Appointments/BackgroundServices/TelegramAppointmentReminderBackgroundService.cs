using Application.Appointments.UseCases;
using Infrastructure.Appointments.Configuration;
using Infrastructure.Telegram.Configuration;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Appointments.BackgroundServices;

public sealed class TelegramAppointmentReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<TelegramReminderOptions> reminderOptions,
    IOptions<TelegramOptions> telegramOptions,
    ILogger<TelegramAppointmentReminderBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reminders = reminderOptions.Value;
        if (!reminders.Enabled || !telegramOptions.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(reminders.PollIntervalMinutes));

        do
        {
            try
            {
                await DispatchAsync(reminders, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Fallo al despachar recordatorios Telegram de citas.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DispatchAsync(
        TelegramReminderOptions reminders,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(
            new DispatchTelegramAppointmentRemindersCommand(
                TimeSpan.FromMinutes(reminders.LeadMinutes),
                TimeSpan.FromMinutes(reminders.GraceMinutes),
                reminders.AllowedStatusNames),
            cancellationToken);

        if (result.Delivered + result.MissingLink > 0)
        {
            logger.LogInformation(
                "Recordatorios Telegram: {Delivered} enviados, {MissingLink} sin vinculo.",
                result.Delivered,
                result.MissingLink);
        }

        if (result.Deferred > 0)
        {
            logger.LogWarning(
                "Quedaron {Deferred} recordatorios Telegram pendientes de reintento.",
                result.Deferred);
        }
    }
}
