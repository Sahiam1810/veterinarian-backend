using Api.ChatAiRunErrors.Dtos;
using Application.ChatAiRunErrors.UseCase;
using ChatAiRunErrorEntity = Domain.ChatAiRunErrors.Entities.ChatAiRunError;

namespace Api.ChatAiRunErrors.Mappings;

public static class ChatAiRunErrorMappings
{
    public static CreateChatAiRunErrorCommand ToCommand(this CreateChatAiRunErrorDto dto)
        => new(
            dto.ChatAiRunId,
            dto.ErrorMessage,
            dto.ErrorCode,
            dto.ProviderErrorId);

    public static ChatAiRunErrorResponseDto ToResponse(this ChatAiRunErrorEntity error)
        => new(
            error.Id,
            error.ChatAiRunId,
            error.ErrorMessage,
            error.ErrorCode,
            error.ProviderErrorId,
            error.CreatedAt);

    public static IReadOnlyCollection<ChatAiRunErrorResponseDto> ToResponse(
        this IReadOnlyCollection<ChatAiRunErrorEntity> errors)
        => errors.Select(error => error.ToResponse()).ToArray();
}
