using Microsoft.AspNetCore.SignalR;

namespace Api.Notifications.Realtime;

public sealed class SubjectUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst("sub")?.Value;
}
