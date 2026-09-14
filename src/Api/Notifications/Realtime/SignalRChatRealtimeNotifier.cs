using Api.Notifications.Hubs;
using Application.Notifications.Abstraction;
using Microsoft.AspNetCore.SignalR;

namespace Api.Notifications.Realtime;

public sealed class SignalRChatRealtimeNotifier(
    IHubContext<NotificationsHub> hubContext) : IChatRealtimeNotifier
{
    private const string EscalationCreatedMethod = "ChatEscalationCreated";
    private const string MessageReceivedMethod = "ChatMessageReceived";
    private const string EscalationResolvedMethod = "ChatEscalationResolved";

    public Task NotifyEscalationCreatedAsync(
        ChatEscalationCreatedPayload payload,
        CancellationToken cancellationToken = default) =>
        Group().SendAsync(EscalationCreatedMethod, payload, cancellationToken);

    public Task NotifyMessageReceivedAsync(
        ChatMessageReceivedPayload payload,
        CancellationToken cancellationToken = default) =>
        Group().SendAsync(MessageReceivedMethod, payload, cancellationToken);

    public Task NotifyEscalationResolvedAsync(
        ChatEscalationResolvedPayload payload,
        CancellationToken cancellationToken = default) =>
        Group().SendAsync(EscalationResolvedMethod, payload, cancellationToken);

    private IClientProxy Group() =>
        hubContext.Clients.Group(NotificationsHubGroups.ReceptionistRole);
}
