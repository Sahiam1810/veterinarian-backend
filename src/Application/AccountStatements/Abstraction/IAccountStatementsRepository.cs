using AccountStatementEntity = Domain.AccountStatements.Entities.AccountStatements;

namespace Application.AccountStatements.Abstraction;

public interface IAccountStatementsRepository
{
    Task<AccountStatementEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AccountStatementEntity>> GetAllByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task AddAsync(
        AccountStatementEntity statement,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        AccountStatementEntity statement,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        AccountStatementEntity statement,
        CancellationToken cancellationToken);
}
