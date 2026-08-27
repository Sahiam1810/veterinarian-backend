namespace Api.Notifications.Dtos;

public sealed record UpdateNotificationRequest(
    Guid UserId,
    Guid AppointmentId,
    string Message,
    DateTime SentAt,
    string Status,
    string Type);
