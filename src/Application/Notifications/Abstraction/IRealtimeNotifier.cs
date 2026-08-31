using Domain.Notifications.Entities;

namespace Application.Notifications.Abstraction;

public interface IRealtimeNotifier
{
    Task NotifyUserAsync(
        Notification notification,
        CancellationToken cancellationToken = default);
}
