namespace Api.ChatEscalationResolutions.Dtos;

public sealed record CreateChatEscalationResolutionDto(
    Guid ChatEscalationId,
    Guid? ResolvedBy,
    string? ResolutionNote,
    DateTime? ResolvedAt);

public sealed record UpdateChatEscalationResolutionDto(
    Guid? ResolvedBy,
    string? ResolutionNote,
    DateTime? ResolvedAt);

public sealed record ChatEscalationResolutionResponseDto(
    Guid Id,
    Guid ChatEscalationId,
    Guid? ResolvedBy,
    string? ResolutionNote,
    DateTime? ResolvedAt);
