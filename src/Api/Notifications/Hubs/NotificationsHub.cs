using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Notifications.Hubs;

[Authorize]
public sealed class NotificationsHub : Hub
{
}
