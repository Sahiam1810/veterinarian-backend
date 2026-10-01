namespace Api.ChatConversations.Dtos;

public sealed record CreateChatConversationDto(
    bool AiEnabled = true,
    string Channel = "Web");

public sealed record UpdateChatConversationAiEnabledDto(bool AiEnabled);

public sealed record CloseChatConversationDto(Guid? ClosedBy);

public sealed record ChatConversationResponseDto(
    Guid Id,
    bool AiEnabled,
    DateTime? LastMessageAt,
    bool Closed,
    DateTime? ClosedAt,
    Guid? ClosedBy,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string Channel,
    string? ClientName = null,
    string? ClientPhone = null);
