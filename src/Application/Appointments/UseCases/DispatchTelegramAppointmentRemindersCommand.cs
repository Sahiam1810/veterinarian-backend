using MediatR;

namespace Application.Appointments.UseCases;

public sealed record DispatchTelegramAppointmentRemindersCommand(
    TimeSpan Lead,
    TimeSpan Grace,
    IReadOnlyCollection<string> AllowedStatusNames)
    : IRequest<TelegramReminderDispatchResult>;

public sealed record TelegramReminderDispatchResult(
    int Delivered,
    int MissingLink,
    int Deferred);
