using MediatR;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Application.UserTokens.UseCase;

public sealed record GetUserTokensByUserIdQuery(Guid UserId)
    : IRequest<IReadOnlyCollection<UserTokenEntity>>;
