namespace Api.ChatEscalationStatusHistories.Dtos;

public sealed record CreateChatEscalationStatusHistoryDto(
    Guid EscalationStatusId,
    Guid ChatEscalationId);

public sealed record UpdateChatEscalationStatusHistoryDto(
    Guid EscalationStatusId);

public sealed record ChatEscalationStatusHistoryResponseDto(
    Guid Id,
    Guid EscalationStatusId,
    Guid ChatEscalationId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
