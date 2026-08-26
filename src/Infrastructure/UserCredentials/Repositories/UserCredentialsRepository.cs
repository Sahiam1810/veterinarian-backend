using Application.UserCredentials.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;

namespace Infrastructure.UserCredentials.Repositories;

public sealed class UserCredentialsRepository : IUserCredentialsRepository
{
    private readonly VeterinaryDbContext _context;

    public UserCredentialsRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public Task<UserCredentialsEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<UserCredentialsEntity>()
            .FirstOrDefaultAsync(
                credentials => credentials.Id == id,
                cancellationToken);

    public Task<UserCredentialsEntity?> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
        => _context.Set<UserCredentialsEntity>()
            .FirstOrDefaultAsync(
                credentials => credentials.AccountId == accountId,
                cancellationToken);

    public Task<bool> ExistsByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
        => _context.Set<UserCredentialsEntity>()
            .AnyAsync(
                credentials => credentials.AccountId == accountId,
                cancellationToken);

    public async Task AddAsync(
        UserCredentialsEntity credentials,
        CancellationToken cancellationToken = default)
        => await _context.Set<UserCredentialsEntity>()
            .AddAsync(credentials, cancellationToken);

    public Task UpdateAsync(
        UserCredentialsEntity credentials,
        CancellationToken cancellationToken = default)
    {
        _context.Set<UserCredentialsEntity>().Update(credentials);
        return Task.CompletedTask;
    }
}
