namespace Api.Notifications.Dtos;

public sealed record NotificationResponse(
    Guid Id,
    Guid UserId,
    string? UserFullName,
    Guid AppointmentId,
    string Message,
    DateTime SentAt,
    string Status,
    string Type,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
