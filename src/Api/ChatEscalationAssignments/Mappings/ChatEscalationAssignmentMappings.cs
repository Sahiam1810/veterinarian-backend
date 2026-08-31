using Api.ChatEscalationAssignments.Dtos;
using Application.ChatEscalationAssignments.UseCase;
using ChatEscalationAssignmentEntity = Domain.ChatEscalationAssignments.Entities.ChatEscalationAssignment;

namespace Api.ChatEscalationAssignments.Mappings;

public static class ChatEscalationAssignmentMappings
{
    public static CreateChatEscalationAssignmentCommand ToCommand(this CreateChatEscalationAssignmentDto dto)
        => new(dto.AgentHumanId, dto.ChatEscalationId, dto.AssignedAt);

    public static UpdateChatEscalationAssignmentCommand ToCommand(
        this UpdateChatEscalationAssignmentDto dto,
        Guid id)
        => new(id, dto.AgentHumanId, dto.AssignedAt);

    public static ChatEscalationAssignmentResponseDto ToResponse(
        this ChatEscalationAssignmentEntity assignment)
        => new(
            assignment.Id,
            assignment.AgentHumanId,
            assignment.ChatEscalationId,
            assignment.AssignedAt);

    public static IReadOnlyCollection<ChatEscalationAssignmentResponseDto> ToResponse(
        this IReadOnlyCollection<ChatEscalationAssignmentEntity> assignments)
        => assignments.Select(assignment => assignment.ToResponse()).ToArray();
}
