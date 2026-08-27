using Application.Common.Abstractions;
using MediatR;
using AccountStatementEntity = Domain.AccountStatements.Entities.AccountStatements;

namespace Application.AccountStatements.UseCases;

public sealed class GetAccountStatementByIdQueryHandler
    : IRequestHandler<GetAccountStatementByIdQuery, AccountStatementEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetAccountStatementByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<AccountStatementEntity?> Handle(
        GetAccountStatementByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.AccountStatementsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);
    }
}
