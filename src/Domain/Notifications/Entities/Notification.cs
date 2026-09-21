using Domain.Appointments.Entities;
using Domain.Clients.Entities;
using Domain.Common;
using Domain.Notifications.ValueObjects;
using UserEntity = Domain.Users.Entities.Users;

namespace Domain.Notifications.Entities;

public sealed class Notification : BaseEntity<Guid>
{
    private Notification()
    {
    }

    // Un aviso pertenece a exactamente uno: un usuario del personal o un cliente.
    private Notification(
        Guid? userId,
        Guid? clientId,
        Guid appointmentId,
        string message,
        DateTime sentAt,
        string status,
        string type)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        ClientId = clientId;
        AppointmentId = appointmentId;
        Message = NotificationMessage.Create(message);
        SentAt = sentAt;
        Status = NotificationStatus.Create(status);
        Type = NotificationType.Create(type);
    }

    // Aviso interno para un usuario del personal (se lista por la API y sale por SignalR).
    public static Notification ForUser(
        Guid userId,
        Guid appointmentId,
        string message,
        DateTime sentAt,
        string status,
        string type)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del usuario es obligatorio.", nameof(userId));
        }

        return new Notification(userId, null, appointmentId, message, sentAt, status, type);
    }

    // Registro de un aviso enviado a un cliente (recordatorio de Telegram). Es interno:
    // la API del personal no lo lista.
    public static Notification ForClient(
        Guid clientId,
        Guid appointmentId,
        string message,
        DateTime sentAt,
        string status,
        string type)
    {
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del cliente es obligatorio.", nameof(clientId));
        }

        return new Notification(null, clientId, appointmentId, message, sentAt, status, type);
    }

    public Guid? UserId { get; private set; }
    public UserEntity? User { get; private set; }

    public Guid? ClientId { get; private set; }
    public ClientEntity? Client { get; private set; }

    public Guid AppointmentId { get; private set; }
    public Appointment? Appointment { get; private set; }

    public NotificationMessage Message { get; private set; } = null!;

    public DateTime SentAt { get; private set; }

    public NotificationStatus Status { get; private set; } = null!;

    public NotificationType Type { get; private set; } = null!;

    // S43: marcar como leída no debe requerir el mismo permiso que editar cualquier campo.
    public void MarkAsRead()
    {
        Status = NotificationStatus.Create("Leída");
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        Guid userId,
        Guid appointmentId,
        string message,
        DateTime sentAt,
        string status,
        string type)
    {
        if (UserId is null)
        {
            throw new InvalidOperationException(
                "Solo se pueden editar los avisos de usuarios del personal.");
        }

        UserId = userId;
        AppointmentId = appointmentId;
        Message = NotificationMessage.Create(message);
        SentAt = sentAt;
        Status = NotificationStatus.Create(status);
        Type = NotificationType.Create(type);
        UpdatedAt = DateTime.UtcNow;
    }
}
