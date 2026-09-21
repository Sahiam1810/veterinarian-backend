namespace Api.ChatParticipants.Dtos;

public sealed record CreateChatParticipantDto(
    Guid ChatConversationId,
    Guid ParticipantTypeId,
    Guid? ClientId,
    Guid? AgentHumanId);

public sealed record ChangeChatParticipantIdentityDto(
    Guid? ClientId,
    Guid? AgentHumanId);

public sealed record ChatParticipantResponseDto(
    Guid Id,
    Guid ChatConversationId,
    Guid ParticipantTypeId,
    Guid? ClientId,
    Guid? AgentHumanId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
