namespace Application.Notifications.Abstraction;

// Ticket B5: broadcast por grupo de rol (hoy, "role:Recepcionista"), a
// diferencia de IRealtimeNotifier.NotifyUserAsync que envía a UN usuario
// puntual y está tipado a la entidad Notification. Los tres métodos envían
// exactamente la forma acordada con frontend en el Contrato de API (§16).
public interface IChatRealtimeNotifier
{
    Task NotifyEscalationCreatedAsync(
        ChatEscalationCreatedPayload payload,
        CancellationToken cancellationToken = default);

    Task NotifyMessageReceivedAsync(
        ChatMessageReceivedPayload payload,
        CancellationToken cancellationToken = default);

    Task NotifyEscalationResolvedAsync(
        ChatEscalationResolvedPayload payload,
        CancellationToken cancellationToken = default);
}

public sealed record ChatEscalationCreatedPayload(
    Guid EscalationId,
    Guid ConversationId,
    Guid? ClientId,
    string? ClientName,
    string? ClientPhone,
    string? Reason,
    string? Priority,
    string? Status,
    string? Channel,
    DateTime CreatedAt,
    string? LastMessage);

public sealed record ChatMessageReceivedPayload(
    Guid MessageId,
    Guid ConversationId,
    string SenderType,
    string? SenderName,
    string Content,
    DateTime SentAt,
    string? MessageType);

public sealed record ChatEscalationResolvedPayload(
    Guid EscalationId,
    Guid? ConversationId,
    string? ResolvedBy,
    DateTime ResolvedAt,
    string? ResolutionNotes);
