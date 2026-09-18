namespace Api.ChatConversations.Dtos;

public sealed record CreateChatConversationDto(
    Guid ConversationStatusId,
    Guid? PriorityId,
    bool AiEnabled = true,
    string Channel = "Web");

public sealed record UpdateChatConversationStatusDto(Guid ConversationStatusId);

public sealed record UpdateChatConversationPriorityDto(Guid? PriorityId);

public sealed record UpdateChatConversationAiEnabledDto(bool AiEnabled);

public sealed record CloseChatConversationDto(Guid? ClosedBy);

public sealed record ChatConversationResponseDto(
    Guid Id,
    Guid ConversationStatusId,
    Guid? PriorityId,
    bool AiEnabled,
    DateTime? LastMessageAt,
    bool Closed,
    DateTime? ClosedAt,
    Guid? ClosedBy,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string Channel,
    // Ticket B7: solo poblados por el listado (GetAll) — Create/Update/GetById
    // siguen usando el mapeo simple y los dejan en null.
    string? ClientName = null,
    string? ClientPhone = null);
