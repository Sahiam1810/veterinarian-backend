using Api.ChatParticipants.Dtos;
using Application.ChatParticipants.UseCase;
using ChatParticipantEntity = Domain.ChatParticipants.Entities.ChatParticipant;

namespace Api.ChatParticipants.Mappings;

public static class ChatParticipantMappings
{
    public static CreateChatParticipantCommand ToCommand(this CreateChatParticipantDto dto)
        => new(
            dto.ChatConversationId,
            dto.ParticipantTypeId,
            dto.ClientId,
            dto.AgentHumanId);

    public static ChangeChatParticipantIdentityCommand ToCommand(
        this ChangeChatParticipantIdentityDto dto,
        Guid id)
        => new(
            id,
            dto.ClientId,
            dto.AgentHumanId);

    public static ChatParticipantResponseDto ToResponse(this ChatParticipantEntity participant)
        => new(
            participant.Id,
            participant.ChatConversationId,
            participant.ParticipantTypeId,
            participant.ClientId,
            participant.AgentHumanId,
            participant.CreatedAt,
            participant.UpdatedAt);

    public static IReadOnlyCollection<ChatParticipantResponseDto> ToResponse(
        this IReadOnlyCollection<ChatParticipantEntity> participants)
        => participants.Select(participant => participant.ToResponse()).ToArray();
}
