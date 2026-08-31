using Api.Notifications.Hubs;
using Api.Notifications.Mappings;
using Application.Notifications.Abstraction;
using Domain.Notifications.Entities;
using Microsoft.AspNetCore.SignalR;

namespace Api.Notifications.Realtime;

public sealed class SignalRRealtimeNotifier(
    IHubContext<NotificationsHub> hubContext) : IRealtimeNotifier
{
    private const string ReceiveNotificationMethod = "ReceiveNotification";

    public Task NotifyUserAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        return hubContext.Clients
            .User(notification.UserId.ToString())
            .SendAsync(
                ReceiveNotificationMethod,
                notification.ToResponse(),
                cancellationToken);
    }
}
