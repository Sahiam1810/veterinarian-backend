using MediatR;

namespace Application.UserTokens.UseCase;

public sealed record CreateUserTokenCommand(
    Guid UserId,
    string TokenValue,
    string TokenType,
    DateTime ExpiresAt) : IRequest<Guid>;
