namespace Api.UserTokens.Dtos;

public sealed record CreateUserTokenRequest(
    Guid AccountId,
    string TokenValue,
    string TokenType,
    DateTime ExpiresAt);
