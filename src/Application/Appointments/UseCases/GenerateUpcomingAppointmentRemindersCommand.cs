using MediatR;

namespace Application.Appointments.UseCases;

public sealed record GenerateUpcomingAppointmentRemindersCommand(
    TimeSpan ReminderWindow,
    IReadOnlyCollection<string> ExcludedStatusNames) : IRequest<int>;
