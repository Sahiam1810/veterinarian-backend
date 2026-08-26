using Api.UserTokens.Dtos;
using Application.UserTokens.UseCase;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Api.UserTokens.Mappings;

public static class UserTokensMappings
{
    public static CreateUserTokenCommand ToCommand(
        this CreateUserTokenRequest request)
    {
        return new CreateUserTokenCommand(
            request.AccountId,
            request.TokenValue,
            request.TokenType,
            request.ExpiresAt);
    }

    public static UserTokenResponse ToResponse(this UserTokenEntity token)
    {
        return new UserTokenResponse(
            token.Id,
            token.AccountId,
            token.TokenType,
            token.ExpiresAt,
            token.IsExpired,
            token.CreatedAt);
    }

    public static IReadOnlyCollection<UserTokenResponse> ToResponse(
        this IReadOnlyCollection<UserTokenEntity> tokens)
    {
        return tokens
            .Select(token => token.ToResponse())
            .ToArray();
    }
}
