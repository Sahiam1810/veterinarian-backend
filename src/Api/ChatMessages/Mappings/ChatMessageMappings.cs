using Api.ChatMessages.Dtos;
using Application.ChatMessages.UseCase;
using ChatMessageEntity = Domain.ChatMessages.Entities.ChatMessage;

namespace Api.ChatMessages.Mappings;

public static class ChatMessageMappings
{
    public static CreateChatMessageCommand ToCommand(this CreateChatMessageDto dto)
        => new(
            dto.ChatConversationId,
            dto.ChatParticipantId,
            dto.SenderTypesId,
            dto.MessageTypeId,
            dto.Content,
            dto.Metadata);

    public static ChatMessageResponseDto ToResponse(this ChatMessageEntity message)
        => new(
            message.Id,
            message.ChatConversationId,
            message.SenderTypesId,
            message.MessageTypeId,
            message.ChatParticipantId,
            message.Content,
            message.Metadata,
            message.CreatedAt);

    public static IReadOnlyCollection<ChatMessageResponseDto> ToResponse(
        this IReadOnlyCollection<ChatMessageEntity> messages)
        => messages.Select(message => message.ToResponse()).ToArray();
}
