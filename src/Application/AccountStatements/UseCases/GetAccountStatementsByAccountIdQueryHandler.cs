using Application.Common.Abstractions;
using MediatR;
using AccountStatementEntity = Domain.AccountStatements.Entities.AccountStatements;

namespace Application.AccountStatements.UseCases;

public sealed class GetAccountStatementsByAccountIdQueryHandler
    : IRequestHandler<
        GetAccountStatementsByAccountIdQuery,
        IReadOnlyCollection<AccountStatementEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAccountStatementsByAccountIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<AccountStatementEntity>> Handle(
        GetAccountStatementsByAccountIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.AccountStatementsRepository.GetAllByAccountIdAsync(
            request.AccountId,
            cancellationToken);
    }
}
