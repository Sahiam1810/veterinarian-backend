namespace Api.Notifications.Hubs;

// Nombre de grupo compartido entre NotificationsHub (quién se une) y
// SignalRChatRealtimeNotifier (a quién se le envía) — evita que un typo en
// uno de los dos lados rompa el broadcast en silencio.
public static class NotificationsHubGroups
{
    public const string ReceptionistRole = "role:Recepcionista";
    public const string SuperAdminRole = "role:SuperAdmin";

    public static readonly IReadOnlyList<string> AdvisorRoles =
    [
        ReceptionistRole,
        SuperAdminRole,
    ];

    public static string ForRole(string role) => $"role:{role}";
}
