namespace Api.UserAccounts.Dtos;

public sealed record UpdateUserAccountRequest(
    string Username,
    string Mail,
    string Status);
