namespace Application.Agent.Messages;

public sealed record AgentConversationContext(
    Guid ConversationId,
    string Channel,
    bool IsEscalated);
