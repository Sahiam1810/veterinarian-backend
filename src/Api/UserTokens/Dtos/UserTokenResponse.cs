namespace Api.UserTokens.Dtos;

public sealed record UserTokenResponse(
    Guid Id,
    Guid UserId,
    string TokenType,
    DateTime ExpiresAt,
    bool IsExpired,
    DateTime CreatedAt);
