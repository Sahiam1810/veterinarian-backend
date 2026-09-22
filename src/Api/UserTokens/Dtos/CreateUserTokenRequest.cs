namespace Api.UserTokens.Dtos;

public sealed record CreateUserTokenRequest(
    Guid UserId,
    string TokenValue,
    string TokenType,
    DateTime ExpiresAt);
