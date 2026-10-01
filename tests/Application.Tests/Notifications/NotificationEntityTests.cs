using Domain.Notifications.Entities;
using Xunit;

namespace Application.Tests.Notifications;

// Un aviso pertenece a exactamente uno: un usuario del personal o un cliente.
public sealed class NotificationEntityTests
{
    private static readonly DateTime SentAt = new(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ForUser_sets_only_the_user()
    {
        var userId = Guid.NewGuid();

        var notification = Notification.ForUser(
            userId, Guid.NewGuid(), "Recordatorio", SentAt, "Pendiente", "Recordatorio");

        Assert.Equal(userId, notification.UserId);
        Assert.Null(notification.ClientId);
    }

    [Fact]
    public void ForClient_sets_only_the_client()
    {
        var clientId = Guid.NewGuid();

        var notification = Notification.ForClient(
            clientId, Guid.NewGuid(), "Recordatorio", SentAt, "Enviado", "Recordatorio1h");

        Assert.Equal(clientId, notification.ClientId);
        Assert.Null(notification.UserId);
    }

    [Fact]
    public void Factories_reject_an_empty_recipient_id()
    {
        Assert.Throws<ArgumentException>(() => Notification.ForUser(
            Guid.Empty, Guid.NewGuid(), "Mensaje", SentAt, "Pendiente", "Recordatorio"));
        Assert.Throws<ArgumentException>(() => Notification.ForClient(
            Guid.Empty, Guid.NewGuid(), "Mensaje", SentAt, "Enviado", "Recordatorio1h"));
    }

    [Fact]
    public void Update_on_a_client_notification_throws()
    {
        var notification = Notification.ForClient(
            Guid.NewGuid(), Guid.NewGuid(), "Mensaje", SentAt, "Enviado", "Recordatorio1h");

        Assert.Throws<InvalidOperationException>(() => notification.Update(
            Guid.NewGuid(), Guid.NewGuid(), "Otro", SentAt, "Pendiente", "Recordatorio"));
    }

    [Fact]
    public void Update_on_a_user_notification_still_works_and_keeps_a_single_recipient()
    {
        var notification = Notification.ForUser(
            Guid.NewGuid(), Guid.NewGuid(), "Mensaje", SentAt, "Pendiente", "Recordatorio");
        var newUserId = Guid.NewGuid();

        notification.Update(newUserId, Guid.NewGuid(), "Nuevo", SentAt, "Leída", "Recordatorio");

        Assert.Equal(newUserId, notification.UserId);
        Assert.Null(notification.ClientId);
        Assert.Equal("Nuevo", notification.Message.Value);
    }
}
