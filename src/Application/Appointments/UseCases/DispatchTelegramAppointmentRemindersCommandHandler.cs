using Application.Appointments.Abstraction;
using Application.Common.Abstractions;
using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Domain.Appointments.Entities;
using Domain.Notifications.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Appointments.UseCases;

public sealed class DispatchTelegramAppointmentRemindersCommandHandler(
    IUnitOfWork unitOfWork,
    ITelegramUnitOfWork telegramUnitOfWork,
    ITelegramBotClient botClient,
    TimeProvider timeProvider,
    IAppointmentBookingSettings bookingSettings,
    ILogger<DispatchTelegramAppointmentRemindersCommandHandler> logger)
    : IRequestHandler<DispatchTelegramAppointmentRemindersCommand, TelegramReminderDispatchResult>
{
    public const string ReminderType = "Recordatorio1h";
    public const string SentStatus = "Enviado";
    public const string MissingLinkStatus = "SinVinculo";

    public async Task<TelegramReminderDispatchResult> Handle(
        DispatchTelegramAppointmentRemindersCommand request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var fromUtc = now.Add(request.Lead - request.Grace);
        var toUtc = now.Add(request.Lead + request.Grace);

        var appointments = await unitOfWork.AppointmentsRepository
            .GetScheduledBetweenAsync(fromUtc, toUtc, cancellationToken);

        appointments = appointments
            .Where(appointment =>
                appointment.ScheduledStart >= fromUtc &&
                appointment.ScheduledStart <= toUtc &&
                appointment.Status is not null &&
                request.AllowedStatusNames.Contains(
                    appointment.Status.Name,
                    StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (appointments.Count == 0)
        {
            return new TelegramReminderDispatchResult(0, 0, 0);
        }

        var notifiedIds = await unitOfWork.NotificationsRepository
            .GetNotifiedAppointmentIdsAsync(
                appointments.Select(appointment => appointment.Id).ToArray(),
                ReminderType,
                cancellationToken);

        var pending = appointments
            .Where(appointment => !notifiedIds.Contains(appointment.Id))
            .ToArray();

        var delivered = 0;
        var missingLink = 0;
        var deferred = 0;
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(bookingSettings.TimeZoneId);

        foreach (var appointment in pending)
        {
            try
            {
                var outcome = await ProcessAsync(appointment, timeZone, now, cancellationToken);
                switch (outcome)
                {
                    case ProcessOutcome.Delivered:
                        delivered++;
                        break;
                    case ProcessOutcome.MissingLink:
                        missingLink++;
                        break;
                    case ProcessOutcome.Deferred:
                        deferred++;
                        break;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(
                    exception,
                    "Fallo al procesar recordatorio Telegram. AppointmentId={AppointmentId}",
                    appointment.Id);
                deferred++;
            }
        }

        return new TelegramReminderDispatchResult(delivered, missingLink, deferred);
    }

    private async Task<ProcessOutcome> ProcessAsync(
        Appointment appointment,
        TimeZoneInfo timeZone,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var ownerUserId = appointment.ClientPet?.Client?.UserId;
        var petName = appointment.ClientPet?.Pet?.Name.Value;
        if (ownerUserId is null || ownerUserId == Guid.Empty || string.IsNullOrWhiteSpace(petName))
        {
            return ProcessOutcome.Deferred;
        }

        var localStart = TimeZoneInfo.ConvertTimeFromUtc(appointment.ScheduledStart, timeZone);
        var message = BuildOwnerMessage(petName, localStart);

        var link = await telegramUnitOfWork.UserLinksRepository
            .GetByPersonIdAsync(ownerUserId.Value, cancellationToken);
        if (link is not { IsActive: true })
        {
            await PersistAsync(
                ownerUserId.Value,
                appointment.Id,
                message,
                now,
                MissingLinkStatus,
                cancellationToken);
            return ProcessOutcome.MissingLink;
        }

        try
        {
            await botClient.SendTextAsync(link.TelegramChatId, message, cancellationToken);
        }
        catch (TelegramDeliveryException exception)
        {
            logger.LogWarning(
                exception,
                "Fallo al enviar recordatorio Telegram. AppointmentId={AppointmentId}",
                appointment.Id);
            return ProcessOutcome.Deferred;
        }

        await PersistAsync(
            ownerUserId.Value,
            appointment.Id,
            message,
            now,
            SentStatus,
            cancellationToken);
        return ProcessOutcome.Delivered;
    }

    private async Task PersistAsync(
        Guid userId,
        Guid appointmentId,
        string message,
        DateTime now,
        string status,
        CancellationToken cancellationToken)
    {
        var notification = new Notification(
            userId,
            appointmentId,
            message,
            now,
            status,
            ReminderType);
        await unitOfWork.NotificationsRepository.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string BuildOwnerMessage(string petName, DateTime localStart) =>
        $"Recordatorio: {petName} tiene cita el {localStart:dd/MM/yyyy HH:mm}. Por favor llega 10 minutos antes.";

    private enum ProcessOutcome
    {
        Delivered,
        MissingLink,
        Deferred
    }
}
