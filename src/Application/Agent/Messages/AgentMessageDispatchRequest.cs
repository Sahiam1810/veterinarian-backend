namespace Application.Agent.Messages;

public sealed record AgentMessageDispatchRequest(
    string Message,
    Guid PersonId,
    Guid? PetId,
    string Language,
    string Role,
    string IdempotencyKey,
    Guid CorrelationId);
