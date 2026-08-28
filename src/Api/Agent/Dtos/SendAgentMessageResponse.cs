namespace Api.Agent.Dtos;

public sealed record SendAgentMessageResponse(
    string? Message,
    Guid ConversationId,
    Guid CorrelationId,
    string ResponseType,
    string? Module);
