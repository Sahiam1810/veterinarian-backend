using Application.Common.Abstractions;
using Application.Notifications.Abstraction;
using Domain.Appointments.Entities;
using Domain.Notifications.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class GenerateUpcomingAppointmentRemindersCommandHandler(
    IUnitOfWork unitOfWork,
    IRealtimeNotifier realtimeNotifier,
    TimeProvider timeProvider)
    : IRequestHandler<GenerateUpcomingAppointmentRemindersCommand, int>
{
    private const string ReminderType = "Recordatorio";
    private const string ReminderStatus = "Pendiente";

    public async Task<int> Handle(
        GenerateUpcomingAppointmentRemindersCommand request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var windowEnd = now.Add(request.ReminderWindow);

        var appointments = await unitOfWork.AppointmentsRepository
            .GetScheduledBetweenAsync(now, windowEnd, cancellationToken);

        if (request.ExcludedStatusNames.Count > 0)
        {
            appointments = appointments
                .Where(appointment => appointment.Status is null ||
                    !request.ExcludedStatusNames.Contains(
                        appointment.Status.Name,
                        StringComparer.OrdinalIgnoreCase))
                .ToArray();
        }

        if (appointments.Count == 0)
        {
            return 0;
        }

        var appointmentIds = appointments
            .Select(appointment => appointment.Id)
            .ToArray();

        var notifiedAppointmentIds = await unitOfWork.NotificationsRepository
            .GetNotifiedAppointmentIdsAsync(appointmentIds, ReminderType, cancellationToken);

        var pendingAppointments = appointments
            .Where(appointment => !notifiedAppointmentIds.Contains(appointment.Id))
            .ToArray();

        var reminders = new List<Notification>(pendingAppointments.Length);

        foreach (var appointment in pendingAppointments)
        {
            var reminder = new Notification(
                appointment.ClientPet!.Client!.UserId,
                appointment.Id,
                BuildMessage(appointment),
                now,
                ReminderStatus,
                ReminderType);

            await unitOfWork.NotificationsRepository.AddAsync(reminder, cancellationToken);
            reminders.Add(reminder);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var reminder in reminders)
        {
            await realtimeNotifier.NotifyUserAsync(reminder, cancellationToken);
        }

        return reminders.Count;
    }

    private static string BuildMessage(Appointment appointment) =>
        $"Recordatorio: tienes una cita para {appointment.ClientPet!.Pet!.Name.Value} " +
        $"el {appointment.ScheduledStart:dd/MM/yyyy HH:mm}.";
}
