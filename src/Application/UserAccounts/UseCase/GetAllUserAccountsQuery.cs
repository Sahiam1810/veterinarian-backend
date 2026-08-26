using MediatR;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.UserAccounts.UseCase;

public sealed record GetAllUserAccountsQuery
    : IRequest<IReadOnlyCollection<UserAccountEntity>>;
