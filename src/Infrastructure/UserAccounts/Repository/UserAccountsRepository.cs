using Application.UserAccounts.Abstraction;
using Domain.UserAccounts.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Infrastructure.UserAccounts.Repository;

public sealed class UserAccountsRepository : IUserAccountsRepository
{
    private readonly VeterinaryDbContext _context;

    public UserAccountsRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public Task<UserAccountEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<UserAccountEntity>()
            .FirstOrDefaultAsync(
                account => account.Id == id,
                cancellationToken);

    public Task<UserAccountEntity?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => _context.Set<UserAccountEntity>()
            .FirstOrDefaultAsync(
                account => account.UserId == userId,
                cancellationToken);

    public async Task<IReadOnlyCollection<UserAccountEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<UserAccountEntity>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default,
        Guid? excludedId = null)
    {
        var accountUsername = AccountUsername.Create(username);

        return _context.Set<UserAccountEntity>()
            .AnyAsync(
                account => account.Username == accountUsername
                    && (!excludedId.HasValue || account.Id != excludedId.Value),
                cancellationToken);
    }

    public Task<bool> ExistsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default,
        Guid? excludedId = null)
        => _context.Set<UserAccountEntity>()
            .AnyAsync(
                account => account.UserId == userId
                    && (!excludedId.HasValue || account.Id != excludedId.Value),
                cancellationToken);

    public async Task AddAsync(
        UserAccountEntity account,
        CancellationToken cancellationToken = default)
        => await _context.Set<UserAccountEntity>()
            .AddAsync(account, cancellationToken);

    public Task UpdateAsync(
        UserAccountEntity account,
        CancellationToken cancellationToken = default)
    {
        _context.Set<UserAccountEntity>().Update(account);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        UserAccountEntity account,
        CancellationToken cancellationToken = default)
    {
        _context.Set<UserAccountEntity>().Remove(account);
        return Task.CompletedTask;
    }
}
