namespace Api.Users.Dtos;

public sealed record UpdateUserRequest(
    string FullName,
    string Email,
    Guid RoleId);
