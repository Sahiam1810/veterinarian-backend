namespace Infrastructure.Chat.Configuration;

public sealed class ChatOptions
{
    public const string SectionName = "Chat";

    // Estado "Resuelta" de ESCALATIONS_STATUSES (sembrado en
    // chat_runtime_catalogs_seed.sql). Ticket B8: se usa al crear una
    // ChatEscalationResolution para sincronizar el estado del ChatEscalation
    // asociado — antes se quedaba en su estado original para siempre.
    public string ResolvedEscalationStatusId { get; init; } = string.Empty;
}
