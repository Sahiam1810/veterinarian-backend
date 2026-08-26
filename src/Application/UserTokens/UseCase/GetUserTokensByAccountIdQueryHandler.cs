using Application.Common.Abstractions;
using MediatR;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Application.UserTokens.UseCase;

public sealed class GetUserTokensByAccountIdQueryHandler
    : IRequestHandler<
        GetUserTokensByAccountIdQuery,
        IReadOnlyCollection<UserTokenEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetUserTokensByAccountIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<UserTokenEntity>> Handle(
        GetUserTokensByAccountIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.UserTokensRepository.GetAllByAccountIdAsync(
            request.AccountId,
            cancellationToken);
    }
}
