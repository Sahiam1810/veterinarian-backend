using Application.Common.Abstractions;
using MediatR;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.UserAccounts.UseCase;

public sealed class GetAllUserAccountsQueryHandler
    : IRequestHandler<
        GetAllUserAccountsQuery,
        IReadOnlyCollection<UserAccountEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllUserAccountsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<UserAccountEntity>> Handle(
        GetAllUserAccountsQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.UserAccountsRepository.GetAllAsync(
            cancellationToken);
    }
}
