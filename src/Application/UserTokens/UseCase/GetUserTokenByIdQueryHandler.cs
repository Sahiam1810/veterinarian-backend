using Application.Common.Abstractions;
using MediatR;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Application.UserTokens.UseCase;

public sealed class GetUserTokenByIdQueryHandler
    : IRequestHandler<GetUserTokenByIdQuery, UserTokenEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetUserTokenByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<UserTokenEntity?> Handle(
        GetUserTokenByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.UserTokensRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
    }
}
