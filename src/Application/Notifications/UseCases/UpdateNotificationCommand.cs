using MediatR;

namespace Application.Notifications.UseCases;

public sealed record UpdateNotificationCommand(
    Guid Id,
    Guid UserId,
    Guid AppointmentId,
    string Message,
    DateTime SentAt,
    string Status,
    string Type) : IRequest<bool>;
