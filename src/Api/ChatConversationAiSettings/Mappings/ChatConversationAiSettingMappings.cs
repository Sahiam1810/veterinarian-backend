using Api.ChatConversationAiSettings.Dtos;
using Application.ChatConversationAiSettings.UseCase;
using ChatConversationAiSettingEntity = Domain.ChatConversationAiSettings.Entities.ChatConversationAiSetting;

namespace Api.ChatConversationAiSettings.Mappings;

public static class ChatConversationAiSettingMappings
{
    public static CreateChatConversationAiSettingCommand ToCommand(this CreateChatConversationAiSettingDto dto)
        => new(dto.ConversationId, dto.AiEnabled, dto.DefaultModelId);

    public static UpdateChatConversationAiSettingCommand ToCommand(
        this UpdateChatConversationAiSettingDto dto,
        Guid id)
        => new(id, dto.AiEnabled, dto.DefaultModelId);

    public static ChatConversationAiSettingResponseDto ToResponse(
        this ChatConversationAiSettingEntity setting)
        => new(
            setting.Id,
            setting.ConversationId,
            setting.AiEnabled,
            setting.DefaultModelId,
            setting.CreatedAt,
            setting.UpdatedAt);

    public static IReadOnlyCollection<ChatConversationAiSettingResponseDto> ToResponse(
        this IReadOnlyCollection<ChatConversationAiSettingEntity> settings)
        => settings.Select(setting => setting.ToResponse()).ToArray();
}
