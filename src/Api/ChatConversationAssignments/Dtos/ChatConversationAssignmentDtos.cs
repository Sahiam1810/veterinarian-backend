namespace Api.ChatConversationAssignments.Dtos;

public sealed record CreateChatConversationAssignmentDto(
    Guid ChatConversationId,
    Guid? AgentHumanId,
    DateTime? AssignedAt);

public sealed record UpdateChatConversationAssignmentDto(
    Guid? AgentHumanId,
    DateTime? AssignedAt,
    DateTime? UnassignedAt);

public sealed record ChatConversationAssignmentResponseDto(
    Guid ChatConversationId,
    Guid? AgentHumanId,
    DateTime? AssignedAt,
    DateTime? UnassignedAt);
