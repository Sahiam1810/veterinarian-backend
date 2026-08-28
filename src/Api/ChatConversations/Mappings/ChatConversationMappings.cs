using Api.ChatConversations.Dtos;
using Application.ChatConversations.UseCase;
using ChatConversationEntity = Domain.ChatConversations.Entities.ChatConversation;

namespace Api.ChatConversations.Mappings;

public static class ChatConversationMappings
{
    public static CreateChatConversationCommand ToCommand(this CreateChatConversationDto dto)
        => new(dto.ConversationStatusId, dto.PriorityId, dto.AiEnabled);

    public static UpdateChatConversationStatusCommand ToCommand(
        this UpdateChatConversationStatusDto dto,
        Guid id)
        => new(id, dto.ConversationStatusId);

    public static UpdateChatConversationPriorityCommand ToCommand(
        this UpdateChatConversationPriorityDto dto,
        Guid id)
        => new(id, dto.PriorityId);

    public static UpdateChatConversationAiEnabledCommand ToCommand(
        this UpdateChatConversationAiEnabledDto dto,
        Guid id)
        => new(id, dto.AiEnabled);

    public static CloseChatConversationCommand ToCommand(
        this CloseChatConversationDto dto,
        Guid id)
        => new(id, dto.ClosedBy);

    public static ChatConversationResponseDto ToResponse(this ChatConversationEntity conversation)
        => new(
            conversation.Id,
            conversation.ConversationStatusId,
            conversation.PriorityId,
            conversation.AiEnabled,
            conversation.LastMessageAt,
            conversation.Closed,
            conversation.ClosedAt,
            conversation.ClosedBy,
            conversation.CreatedAt,
            conversation.UpdatedAt);

    public static IReadOnlyCollection<ChatConversationResponseDto> ToResponse(
        this IReadOnlyCollection<ChatConversationEntity> conversations)
        => conversations.Select(conversation => conversation.ToResponse()).ToArray();
}
