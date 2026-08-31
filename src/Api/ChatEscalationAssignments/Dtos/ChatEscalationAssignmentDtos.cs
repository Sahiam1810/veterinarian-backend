namespace Api.ChatEscalationAssignments.Dtos;

public sealed record CreateChatEscalationAssignmentDto(
    Guid AgentHumanId,
    Guid ChatEscalationId,
    DateTime? AssignedAt);

public sealed record UpdateChatEscalationAssignmentDto(
    Guid AgentHumanId,
    DateTime? AssignedAt);

public sealed record ChatEscalationAssignmentResponseDto(
    Guid Id,
    Guid AgentHumanId,
    Guid ChatEscalationId,
    DateTime? AssignedAt);
