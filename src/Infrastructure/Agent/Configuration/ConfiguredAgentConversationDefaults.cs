using Application.Agent.Abstractions;

namespace Infrastructure.Agent.Configuration;

public sealed record ConfiguredAgentConversationDefaults(
    Guid InitialConversationStatusId,
    Guid ClientParticipantTypeId) : IAgentConversationDefaults;
