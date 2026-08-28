namespace Application.Agent.Messages;

public sealed record AgentMessageResult(
    string? Message,
    Guid ConversationId,
    Guid CorrelationId,
    string ResponseType,
    string? Module);
