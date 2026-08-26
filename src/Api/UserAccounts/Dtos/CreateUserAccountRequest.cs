namespace Api.UserAccounts.Dtos;

public sealed record CreateUserAccountRequest(
    Guid UserId,
    string Username,
    string Mail,
    string Status);
