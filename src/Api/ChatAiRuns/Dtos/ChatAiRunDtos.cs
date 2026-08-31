namespace Api.ChatAiRuns.Dtos;

public sealed record CreateChatAiRunDto(
    Guid ChatConversationId,
    Guid ChatMessageId,
    Guid AiModelId,
    Guid AiRunStatusId);

public sealed record UpdateChatAiRunStatusDto(Guid AiRunStatusId);

public sealed record ChatAiRunResponseDto(
    Guid Id,
    Guid ChatConversationId,
    Guid ChatMessageId,
    Guid AiModelId,
    Guid AiRunStatusId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
