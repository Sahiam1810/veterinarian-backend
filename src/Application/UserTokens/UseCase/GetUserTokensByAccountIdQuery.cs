using MediatR;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Application.UserTokens.UseCase;

public sealed record GetUserTokensByAccountIdQuery(Guid AccountId)
    : IRequest<IReadOnlyCollection<UserTokenEntity>>;
