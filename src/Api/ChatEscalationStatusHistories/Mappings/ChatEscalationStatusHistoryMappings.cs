using Api.ChatEscalationStatusHistories.Dtos;
using Application.ChatEscalationStatusHistories.UseCase;
using ChatEscalationStatusHistoryEntity = Domain.ChatEscalationStatusHistories.Entities.ChatEscalationStatusHistory;

namespace Api.ChatEscalationStatusHistories.Mappings;

public static class ChatEscalationStatusHistoryMappings
{
    public static CreateChatEscalationStatusHistoryCommand ToCommand(
        this CreateChatEscalationStatusHistoryDto dto)
        => new(dto.EscalationStatusId, dto.ChatEscalationId);

    public static UpdateChatEscalationStatusHistoryCommand ToCommand(
        this UpdateChatEscalationStatusHistoryDto dto,
        Guid id)
        => new(id, dto.EscalationStatusId);

    public static ChatEscalationStatusHistoryResponseDto ToResponse(
        this ChatEscalationStatusHistoryEntity history)
        => new(
            history.Id,
            history.EscalationStatusId,
            history.ChatEscalationId,
            history.CreatedAt,
            history.UpdatedAt);

    public static IReadOnlyCollection<ChatEscalationStatusHistoryResponseDto> ToResponse(
        this IReadOnlyCollection<ChatEscalationStatusHistoryEntity> histories)
        => histories.Select(history => history.ToResponse()).ToArray();
}
