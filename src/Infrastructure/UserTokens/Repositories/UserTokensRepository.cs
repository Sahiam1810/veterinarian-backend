using Application.UserTokens.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Infrastructure.UserTokens.Repositories;

public sealed class UserTokensRepository : IUserTokensRepository
{
    private readonly VeterinaryDbContext _context;

    public UserTokensRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public Task<UserTokenEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<UserTokenEntity>()
            .FirstOrDefaultAsync(
                token => token.Id == id,
                cancellationToken);

    public Task<UserTokenEntity?> GetByTokenValueAsync(
        string tokenValue,
        CancellationToken cancellationToken = default)
        => _context.Set<UserTokenEntity>()
            .FirstOrDefaultAsync(
                token => token.TokenValue == tokenValue,
                cancellationToken);

    public async Task<IReadOnlyCollection<UserTokenEntity>> GetAllByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
        => await _context.Set<UserTokenEntity>()
            .AsNoTracking()
            .Where(token => token.AccountId == accountId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        UserTokenEntity token,
        CancellationToken cancellationToken = default)
        => await _context.Set<UserTokenEntity>()
            .AddAsync(token, cancellationToken);

    public Task DeleteAsync(
        UserTokenEntity token,
        CancellationToken cancellationToken = default)
    {
        _context.Set<UserTokenEntity>().Remove(token);
        return Task.CompletedTask;
    }
}
