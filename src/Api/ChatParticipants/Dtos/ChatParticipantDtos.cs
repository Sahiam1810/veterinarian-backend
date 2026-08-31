namespace Api.ChatParticipants.Dtos;

public sealed record CreateChatParticipantDto(
    Guid ChatConversationId,
    Guid ParticipantTypeId,
    Guid? ChatUserProfileId,
    Guid? AgentHumanId,
    Guid? AiModelId);

public sealed record ChangeChatParticipantIdentityDto(
    Guid? ChatUserProfileId,
    Guid? AgentHumanId,
    Guid? AiModelId);

public sealed record ChatParticipantResponseDto(
    Guid Id,
    Guid ChatConversationId,
    Guid ParticipantTypeId,
    Guid? ChatUserProfileId,
    Guid? AgentHumanId,
    Guid? AiModelId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
