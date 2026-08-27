using Domain.Appointments.Entities;
using Domain.Common;
using Domain.Notifications.ValueObjects;
using UserEntity = Domain.Users.Entities.Users;

namespace Domain.Notifications.Entities;

public sealed class Notification : BaseEntity<Guid>
{
    private Notification()
    {
    }

    public Notification(
        Guid userId,
        Guid appointmentId,
        string message,
        DateTime sentAt,
        string status,
        string type)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        AppointmentId = appointmentId;
        Message = NotificationMessage.Create(message);
        SentAt = sentAt;
        Status = NotificationStatus.Create(status);
        Type = NotificationType.Create(type);
    }

    public Guid UserId { get; private set; }
    public UserEntity? User { get; private set; }

    public Guid AppointmentId { get; private set; }
    public Appointment? Appointment { get; private set; }

    public NotificationMessage Message { get; private set; } = null!;

    public DateTime SentAt { get; private set; }

    public NotificationStatus Status { get; private set; } = null!;

    public NotificationType Type { get; private set; } = null!;

    public void Update(
        Guid userId,
        Guid appointmentId,
        string message,
        DateTime sentAt,
        string status,
        string type)
    {
        UserId = userId;
        AppointmentId = appointmentId;
        Message = NotificationMessage.Create(message);
        SentAt = sentAt;
        Status = NotificationStatus.Create(status);
        Type = NotificationType.Create(type);
        UpdatedAt = DateTime.UtcNow;
    }
}
