using MediatR;

namespace Application.UserTokens.UseCase;

public sealed record DeleteUserTokenCommand(Guid Id) : IRequest<bool>;
