using Application.Common.Abstractions;
using MediatR;
using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;

namespace Application.UserCredentials.UseCase;

public sealed class GetUserCredentialsByAccountIdQueryHandler
    : IRequestHandler<GetUserCredentialsByAccountIdQuery, UserCredentialsEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetUserCredentialsByAccountIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<UserCredentialsEntity?> Handle(
        GetUserCredentialsByAccountIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.UserCredentialsRepository.GetByAccountIdAsync(
            request.AccountId,
            cancellationToken);
    }
}
