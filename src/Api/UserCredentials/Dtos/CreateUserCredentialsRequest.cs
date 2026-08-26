namespace Api.UserCredentials.Dtos;

public sealed record CreateUserCredentialsRequest(
    Guid AccountId,
    string Password);
