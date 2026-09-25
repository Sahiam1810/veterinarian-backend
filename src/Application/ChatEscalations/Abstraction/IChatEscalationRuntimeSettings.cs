namespace Application.ChatEscalations.Abstraction;

public interface IChatEscalationRuntimeSettings
{
    // Estado "Resuelta" de ESCALATIONS_STATUSES, configurable en vez de
    // hardcodeado — mismo criterio que Telegram__PendingEscalationStatusId
    // (Ticket B2). Se usa al crear una ChatEscalationResolution para
    // sincronizar el estado del ChatEscalation asociado (Ticket B8).
    Guid ResolvedEscalationStatusId { get; }
}
