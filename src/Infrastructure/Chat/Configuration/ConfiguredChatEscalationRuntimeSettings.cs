using Application.ChatEscalations.Abstraction;

namespace Infrastructure.Chat.Configuration;

public sealed record ConfiguredChatEscalationRuntimeSettings(
    Guid ResolvedEscalationStatusId) : IChatEscalationRuntimeSettings;
