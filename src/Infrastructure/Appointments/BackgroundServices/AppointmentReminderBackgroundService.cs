using Application.Appointments.UseCases;
using Infrastructure.Appointments.Configuration;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Appointments.BackgroundServices;

public sealed class AppointmentReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ReminderOptions> options,
    ILogger<AppointmentReminderBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reminderOptions = options.Value;
        if (!reminderOptions.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(reminderOptions.PollIntervalMinutes));

        do
        {
            try
            {
                await GenerateRemindersAsync(reminderOptions, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Fallo al generar recordatorios de citas próximas.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task GenerateRemindersAsync(
        ReminderOptions reminderOptions,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var generated = await sender.Send(
            new GenerateUpcomingAppointmentRemindersCommand(
                TimeSpan.FromHours(reminderOptions.WindowHours),
                reminderOptions.ExcludedStatusNames),
            cancellationToken);

        if (generated > 0)
        {
            logger.LogInformation(
                "Se generaron {Count} recordatorio(s) de citas próximas.",
                generated);
        }
    }
}
