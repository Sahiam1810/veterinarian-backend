using Application.Common.Abstractions;
using MediatR;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Application.UserTokens.UseCase;

public sealed class GetUserTokensByUserIdQueryHandler
    : IRequestHandler<
        GetUserTokensByUserIdQuery,
        IReadOnlyCollection<UserTokenEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetUserTokensByUserIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<UserTokenEntity>> Handle(
        GetUserTokensByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.UserTokensRepository.GetAllByUserIdAsync(
            request.UserId,
            cancellationToken);
    }
}
