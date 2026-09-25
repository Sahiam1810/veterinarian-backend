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

public sealed record ResolveChatEscalationDto(
    Guid EscalationStatusId,
    Guid ResolvedBy,
    string? ResolutionNote);

public sealed record ChatEscalationResponseDto(
    Guid Id,
    Guid ChatConversationId,
    Guid EscalationStatusId,
    bool FromAi,
    string? Reason,
    DateTime CreatedAt,
    string? UpdateAt,
    DateTime? ResolvedAt = null,
    Guid? ResolvedBy = null,
    string? ResolutionNote = null);
