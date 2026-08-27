using MediatR;
using AccountStatementEntity = Domain.AccountStatements.Entities.AccountStatements;

namespace Application.AccountStatements.UseCases;

public sealed record GetAccountStatementByIdQuery(Guid Id)
    : IRequest<AccountStatementEntity?>;
