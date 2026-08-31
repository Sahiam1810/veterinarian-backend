using Microsoft.AspNetCore.SignalR;

namespace Api.Notifications.Realtime;

public sealed class PersonIdUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst("person_id")?.Value;
}
