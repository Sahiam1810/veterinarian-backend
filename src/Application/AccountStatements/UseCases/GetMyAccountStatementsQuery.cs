using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using AccountStatementEntity = Domain.AccountStatements.Entities.AccountStatements;

namespace Application.AccountStatements.UseCases;

public sealed record GetMyAccountStatementsQuery(Guid UserAccountId)
    : IRequest<IReadOnlyCollection<AccountStatementEntity>>;

public sealed class GetMyAccountStatementsQueryHandler
    : IRequestHandler<GetMyAccountStatementsQuery, IReadOnlyCollection<AccountStatementEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetMyAccountStatementsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<AccountStatementEntity>> Handle(
        GetMyAccountStatementsQuery request,
        CancellationToken cancellationToken)
    {
        var account = await _uow.UserAccountsRepository.GetByIdAsync(
            request.UserAccountId,
            cancellationToken);

        if (account is null)
        {
            throw new NotFoundException("Cuenta de usuario no encontrada.");
        }

        var client = await _uow.ClientsRepository.GetByUserIdAsync(
            account.UserId,
            cancellationToken);

        if (client is null)
        {
            return Array.Empty<AccountStatementEntity>();
        }

        return await _uow.AccountStatementsRepository.GetAllByAccountIdAsync(
            account.Id,
            cancellationToken);
    }
}
