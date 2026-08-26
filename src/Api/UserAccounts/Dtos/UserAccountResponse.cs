namespace Api.UserAccounts.Dtos;

public sealed record UserAccountResponse(
    Guid Id,
    Guid UserId,
    string Username,
    string Mail,
    string Status,
    DateTime CreatedAt);
