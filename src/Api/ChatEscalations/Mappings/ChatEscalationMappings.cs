using Api.ChatEscalations.Dtos;
using Application.ChatEscalations.UseCase;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;

namespace Api.ChatEscalations.Mappings;

public static class ChatEscalationMappings
{
    public static CreateChatEscalationCommand ToCommand(this CreateChatEscalationDto dto)
        => new(dto.ChatConversationId, dto.EscalationStatusId, dto.FromAi, dto.Reason, dto.UpdateAt);

    public static UpdateChatEscalationCommand ToCommand(
        this UpdateChatEscalationDto dto,
        Guid id)
        => new(id, dto.EscalationStatusId, dto.FromAi, dto.Reason, dto.UpdateAt);

    public static ChatEscalationResponseDto ToResponse(this ChatEscalationEntity escalation)
        => new(
            escalation.Id,
            escalation.ChatConversationId,
            escalation.EscalationStatusId,
            escalation.FromAi,
            escalation.Reason,
            escalation.CreatedAt,
            escalation.UpdateAt);

    public static IReadOnlyCollection<ChatEscalationResponseDto> ToResponse(
        this IReadOnlyCollection<ChatEscalationEntity> escalations)
        => escalations.Select(escalation => escalation.ToResponse()).ToArray();
}
