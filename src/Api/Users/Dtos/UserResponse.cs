namespace Api.Users.Dtos;

public sealed record UserResponse(
    Guid Id,
    string FullName,
    string Email,
    Guid RoleId,
    bool IsActive,
    DateTime CreatedAt);
