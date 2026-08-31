using Api.ChatEscalationResolutions.Dtos;
using Application.ChatEscalationResolutions.UseCase;
using ChatEscalationResolutionEntity = Domain.ChatEscalationResolutions.Entities.ChatEscalationResolution;

namespace Api.ChatEscalationResolutions.Mappings;

public static class ChatEscalationResolutionMappings
{
    public static CreateChatEscalationResolutionCommand ToCommand(this CreateChatEscalationResolutionDto dto)
        => new(dto.ChatEscalationId, dto.ResolvedBy, dto.ResolutionNote, dto.ResolvedAt);

    public static UpdateChatEscalationResolutionCommand ToCommand(
        this UpdateChatEscalationResolutionDto dto,
        Guid id)
        => new(id, dto.ResolvedBy, dto.ResolutionNote, dto.ResolvedAt);

    public static ChatEscalationResolutionResponseDto ToResponse(
        this ChatEscalationResolutionEntity resolution)
        => new(
            resolution.Id,
            resolution.ChatEscalationId,
            resolution.ResolvedBy,
            resolution.ResolutionNote,
            resolution.ResolvedAt);

    public static IReadOnlyCollection<ChatEscalationResolutionResponseDto> ToResponse(
        this IReadOnlyCollection<ChatEscalationResolutionEntity> resolutions)
        => resolutions.Select(resolution => resolution.ToResponse()).ToArray();
}
