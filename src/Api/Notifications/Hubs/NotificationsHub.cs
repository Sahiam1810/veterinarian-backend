using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Notifications.Hubs;

[Authorize]
public sealed class NotificationsHub : Hub
{
    // Ticket B5: une la conexión a su grupo de rol (mismo claim "role" que ya
    // usa RoleClaimType en JwtAuthenticationExtensions) para poder hacer
    // broadcast a "todas las Recepcionistas conectadas". Al desconectar, el
    // socket sale del grupo automáticamente — no hace falta OnDisconnectedAsync.
    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst("role")?.Value;
        if (!string.IsNullOrWhiteSpace(role))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, NotificationsHubGroups.ForRole(role));
        }

        await base.OnConnectedAsync();
    }
}
