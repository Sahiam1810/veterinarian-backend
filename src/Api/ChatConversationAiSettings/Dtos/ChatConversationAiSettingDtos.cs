namespace Api.ChatConversationAiSettings.Dtos;

public sealed record CreateChatConversationAiSettingDto(
    Guid ConversationId,
    bool AiEnabled,
    Guid? DefaultModelId);

public sealed record UpdateChatConversationAiSettingDto(
    bool AiEnabled,
    Guid? DefaultModelId);

public sealed record ChatConversationAiSettingResponseDto(
    Guid Id,
    Guid ConversationId,
    bool AiEnabled,
    Guid? DefaultModelId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
