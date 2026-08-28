namespace Application.Agent.Messages;

public sealed record AgentMessageEnvelope(
    string Message,
    Guid ConversationId,
    Guid UserId,
    Guid? PetId,
    string Channel,
    string Language,
    IReadOnlyList<string> Roles,
    bool IsEscalated,
    Guid CorrelationId,
    string IdempotencyKey,
    bool PublishAsGlobalKnowledge);
