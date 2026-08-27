using Api.Notifications.Dtos;
using Application.Notifications.UseCases;
using Domain.Notifications.Entities;

namespace Api.Notifications.Mappings;

public static class NotificationMappings
{
    public static CreateNotificationCommand ToCommand(
        this CreateNotificationRequest request)
    {
        return new CreateNotificationCommand(
            request.UserId,
            request.AppointmentId,
            request.Message,
            request.SentAt,
            request.Status,
            request.Type);
    }

    public static UpdateNotificationCommand ToCommand(
        this UpdateNotificationRequest request,
        Guid id)
    {
        return new UpdateNotificationCommand(
            id,
            request.UserId,
            request.AppointmentId,
            request.Message,
            request.SentAt,
            request.Status,
            request.Type);
    }

    public static NotificationResponse ToResponse(
        this Notification entity)
    {
        return new NotificationResponse(
            entity.Id,
            entity.UserId,
            entity.User?.FullName,
            entity.AppointmentId,
            entity.Message.Value,
            entity.SentAt,
            entity.Status.Value,
            entity.Type.Value,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    public static IReadOnlyCollection<NotificationResponse> ToResponse(
        this IReadOnlyCollection<Notification> entities)
    {
        return entities
            .Select(e => e.ToResponse())
            .ToArray();
    }
}
