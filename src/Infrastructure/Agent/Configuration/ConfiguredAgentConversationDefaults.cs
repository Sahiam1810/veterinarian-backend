using Application.Agent.Abstractions;

namespace Infrastructure.Agent.Configuration;

public sealed record ConfiguredAgentConversationDefaults(
    Guid ClientParticipantTypeId) : IAgentConversationDefaults;
