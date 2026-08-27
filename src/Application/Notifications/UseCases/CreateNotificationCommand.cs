using MediatR;

namespace Application.Notifications.UseCases;

public sealed record CreateNotificationCommand(
    Guid UserId,
    Guid AppointmentId,
    string Message,
    DateTime SentAt,
    string Status,
    string Type) : IRequest<Guid>;
