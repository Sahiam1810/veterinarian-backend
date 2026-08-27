namespace Api.ChatUserProfiles.Dtos;

public sealed record CreateChatUserProfileDto(
    Guid PersonId,
    string? DisplayName,
    string? AvatarUrl,
    string? Bio);

public sealed record UpdateChatUserProfileDto(
    string? DisplayName,
    string? AvatarUrl,
    string? Bio);

public sealed record ChatUserProfileResponseDto(
    Guid Id,
    Guid PersonId,
    string? DisplayName,
    string? AvatarUrl,
    string? Bio,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
