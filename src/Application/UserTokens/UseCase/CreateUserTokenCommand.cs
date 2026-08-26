using MediatR;

namespace Application.UserTokens.UseCase;

public sealed record CreateUserTokenCommand(
    Guid AccountId,
    string TokenValue,
    string TokenType,
    DateTime ExpiresAt) : IRequest<Guid>;
