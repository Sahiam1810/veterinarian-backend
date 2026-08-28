using Api.ChatUserProfiles.Dtos;
using Application.ChatUserProfiles.UseCase;
using ChatUserProfileEntity = Domain.ChatUserProfiles.Entities.ChatUserProfile;

namespace Api.ChatUserProfiles.Mappings;

public static class ChatUserProfileMappings
{
    public static CreateChatUserProfileCommand ToCommand(this CreateChatUserProfileDto dto)
        => new(dto.UserId, dto.DisplayName, dto.AvatarUrl, dto.Bio);

    public static UpdateChatUserProfileCommand ToCommand(this UpdateChatUserProfileDto dto, Guid id)
        => new(id, dto.DisplayName, dto.AvatarUrl, dto.Bio);

    public static ChatUserProfileResponseDto ToResponse(this ChatUserProfileEntity profile)
        => new(
            profile.Id,
            profile.UserId,
            profile.DisplayName.Value,
            profile.AvatarUrl.Value,
            profile.Bio.Value,
            profile.CreatedAt,
            profile.UpdatedAt);

    public static IReadOnlyCollection<ChatUserProfileResponseDto> ToResponse(
        this IReadOnlyCollection<ChatUserProfileEntity> profiles)
        => profiles.Select(profile => profile.ToResponse()).ToArray();
}
