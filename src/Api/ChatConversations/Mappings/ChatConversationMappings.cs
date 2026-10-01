using Api.ChatConversations.Dtos;
using Application.ChatConversations.UseCase;
using ChatConversationEntity = Domain.ChatConversations.Entities.ChatConversation;

namespace Api.ChatConversations.Mappings;

public static class ChatConversationMappings
{
    public static CreateChatConversationCommand ToCommand(this CreateChatConversationDto dto)
        => new(dto.AiEnabled, dto.Channel);

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
            conversation.AiEnabled,
            conversation.LastMessageAt,
            conversation.Closed,
            conversation.ClosedAt,
            conversation.ClosedBy,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            conversation.Channel);

    public static IReadOnlyCollection<ChatConversationResponseDto> ToResponse(
        this IReadOnlyCollection<ChatConversationEntity> conversations)
        => conversations.Select(conversation => conversation.ToResponse()).ToArray();

    // Ticket B7
    public static ChatConversationResponseDto ToResponse(this ChatConversationWithClient item)
        => item.Conversation.ToResponse() with
        {
            ClientName = item.ClientName,
            ClientPhone = item.ClientPhone,
        };

    public static IReadOnlyCollection<ChatConversationResponseDto> ToResponse(
        this IReadOnlyCollection<ChatConversationWithClient> items)
        => items.Select(item => item.ToResponse()).ToArray();
}
