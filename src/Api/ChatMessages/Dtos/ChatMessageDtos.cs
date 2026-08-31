namespace Api.ChatMessages.Dtos;

public sealed record CreateChatMessageDto(
    Guid ChatConversationId,
    Guid ChatParticipantId,
    Guid SenderTypesId,
    Guid MessageTypeId,
    string Content,
    string? Metadata);

public sealed record ChatMessageResponseDto(
    Guid Id,
    Guid ChatConversationId,
    Guid SenderTypesId,
    Guid MessageTypeId,
    Guid ChatParticipantId,
    string Content,
    string? Metadata,
    DateTime CreatedAt);
