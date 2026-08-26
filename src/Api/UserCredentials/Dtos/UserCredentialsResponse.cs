namespace Api.UserCredentials.Dtos;

public sealed record UserCredentialsResponse(
    Guid Id,
    Guid AccountId,
    DateTime LastChanged,
    DateTime CreatedAt);
