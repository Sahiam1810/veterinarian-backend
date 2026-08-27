using MediatR;
using AccountStatementEntity = Domain.AccountStatements.Entities.AccountStatements;

namespace Application.AccountStatements.UseCases;

public sealed record GetAccountStatementsByAccountIdQuery(Guid AccountId)
    : IRequest<IReadOnlyCollection<AccountStatementEntity>>;
