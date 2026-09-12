using MediatR;

namespace Application.Notifications.UseCases;

public sealed record MarkNotificationAsReadCommand(
    Guid NotificationId,
    Guid ActorPersonId) : IRequest;
