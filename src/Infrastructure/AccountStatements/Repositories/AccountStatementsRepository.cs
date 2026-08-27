using Application.AccountStatements.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using AccountStatementEntity = Domain.AccountStatements.Entities.AccountStatements;

namespace Infrastructure.AccountStatements.Repositories;

public sealed class AccountStatementsRepository : IAccountStatementsRepository
{
    private readonly VeterinaryDbContext _context;

    public AccountStatementsRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public Task<AccountStatementEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<AccountStatementEntity>()
            .FirstOrDefaultAsync(
                statement => statement.Id == id,
                cancellationToken);

    public async Task<IReadOnlyCollection<AccountStatementEntity>> GetAllByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
        => await _context.Set<AccountStatementEntity>()
            .AsNoTracking()
            .Where(statement => statement.AccountId == accountId)
            .OrderByDescending(statement => statement.IssueDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        AccountStatementEntity statement,
        CancellationToken cancellationToken = default)
        => await _context.Set<AccountStatementEntity>()
            .AddAsync(statement, cancellationToken);

    public Task UpdateAsync(
        AccountStatementEntity statement,
        CancellationToken cancellationToken = default)
    {
        _context.Set<AccountStatementEntity>().Update(statement);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        AccountStatementEntity statement,
        CancellationToken cancellationToken = default)
    {
        _context.Set<AccountStatementEntity>().Remove(statement);
        return Task.CompletedTask;
    }
}
