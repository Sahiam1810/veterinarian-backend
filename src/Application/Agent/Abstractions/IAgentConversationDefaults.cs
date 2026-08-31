namespace Application.Agent.Abstractions;

public interface IAgentConversationDefaults
{
    Guid InitialConversationStatusId { get; }

    Guid ClientParticipantTypeId { get; }
}
