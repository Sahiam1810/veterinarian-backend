namespace Api.Users.Dtos;

public sealed record CreateUserRequest(
    string FullName,
    string Email,
    string Password,
    Guid RoleId);
