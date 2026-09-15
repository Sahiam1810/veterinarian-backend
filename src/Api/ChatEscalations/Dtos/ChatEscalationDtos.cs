namespace Api.ChatEscalations.Dtos;

public sealed record CreateChatEscalationDto(
    Guid ChatConversationId,
    Guid EscalationStatusId,
    bool FromAi,
    string? Reason,
    string? UpdateAt);

public sealed record UpdateChatEscalationDto(
    Guid EscalationStatusId,
    bool FromAi,
    string? Reason,
    string? UpdateAt);

public sealed record ChatEscalationResponseDto(
    Guid Id,
    Guid ChatConversationId,
    Guid EscalationStatusId,
    bool FromAi,
    string? Reason,
    DateTime CreatedAt,
    string? UpdateAt,
    // Ticket B7: la prioridad vive en ChatConversation, no en ChatEscalation —
    // solo poblados por el listado (GetAll); Create/Update/GetById quedan en null.
    Guid? PriorityId = null,
    string? Priority = null);
