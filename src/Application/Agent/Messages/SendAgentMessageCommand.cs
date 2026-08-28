using MediatR;

namespace Application.Agent.Messages;

public sealed record SendAgentMessageCommand(
    string Message,
    Guid? ConversationId,
    Guid? PetId,
    string Language,
    Guid PersonId,
    string Role,
    string IdempotencyKey,
    Guid CorrelationId) : IRequest<AgentMessageResult>;
