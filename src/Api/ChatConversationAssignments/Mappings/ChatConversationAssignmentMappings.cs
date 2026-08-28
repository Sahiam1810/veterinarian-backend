using Api.ChatConversationAssignments.Dtos;
using Application.ChatConversationAssignments.UseCase;
using ChatConversationAssignmentEntity = Domain.ChatConversationAssignments.Entities.ChatConversationAssignment;

namespace Api.ChatConversationAssignments.Mappings;

public static class ChatConversationAssignmentMappings
{
    public static CreateChatConversationAssignmentCommand ToCommand(this CreateChatConversationAssignmentDto dto)
        => new(dto.ChatConversationId, dto.AgentHumanId, dto.AssignedAt);

    public static UpdateChatConversationAssignmentCommand ToCommand(
        this UpdateChatConversationAssignmentDto dto,
        Guid chatConversationId)
        => new(chatConversationId, dto.AgentHumanId, dto.AssignedAt, dto.UnassignedAt);

    public static ChatConversationAssignmentResponseDto ToResponse(
        this ChatConversationAssignmentEntity assignment)
        => new(
            assignment.ChatConversationId,
            assignment.AgentHumanId,
            assignment.AssignedAt,
            assignment.UnassignedAt);

    public static IReadOnlyCollection<ChatConversationAssignmentResponseDto> ToResponse(
        this IReadOnlyCollection<ChatConversationAssignmentEntity> assignments)
        => assignments.Select(assignment => assignment.ToResponse()).ToArray();
}
