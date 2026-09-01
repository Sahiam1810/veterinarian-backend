using MediatR;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Application.UserTokens.UseCase;

public sealed record GetUserTokenByIdQuery(Guid Id)
    : IRequest<UserTokenEntity>;
