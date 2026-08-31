using Api.ChatAiRuns.Dtos;
using Application.ChatAiRuns.UseCase;
using ChatAiRunEntity = Domain.ChatAiRuns.Entities.ChatAiRun;

namespace Api.ChatAiRuns.Mappings;

public static class ChatAiRunMappings
{
    public static CreateChatAiRunCommand ToCommand(this CreateChatAiRunDto dto)
        => new(
            dto.ChatConversationId,
            dto.ChatMessageId,
            dto.AiModelId,
            dto.AiRunStatusId);

    public static UpdateChatAiRunStatusCommand ToCommand(
        this UpdateChatAiRunStatusDto dto,
        Guid id)
        => new(id, dto.AiRunStatusId);

    public static ChatAiRunResponseDto ToResponse(this ChatAiRunEntity run)
        => new(
            run.Id,
            run.ChatConversationId,
            run.ChatMessageId,
            run.AiModelId,
            run.AiRunStatusId,
            run.CreatedAt,
            run.UpdatedAt ?? run.CreatedAt);

    public static IReadOnlyCollection<ChatAiRunResponseDto> ToResponse(
        this IReadOnlyCollection<ChatAiRunEntity> runs)
        => runs.Select(run => run.ToResponse()).ToArray();
}
