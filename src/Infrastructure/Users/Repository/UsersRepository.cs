using Application.Users.Abstraction;
using Domain.Users.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UserEntity = Domain.Users.Entities.Users;

namespace Infrastructure.Users.Repository;

public sealed class UsersRepository : IUsersRepository
{
    private readonly VeterinaryDbContext _context;

    public UsersRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public Task<UserEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<UserEntity>()
            .FirstOrDefaultAsync(
                user => user.Id == id,
                cancellationToken);

    public async Task<IReadOnlyCollection<UserEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<UserEntity>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken = default,
        Guid? excludedId = null)
    {
        var userEmail = UserEmail.Create(email);

        return _context.Set<UserEntity>()
            .AnyAsync(
                user => user.Email == userEmail
                    && (!excludedId.HasValue || user.Id != excludedId.Value),
                cancellationToken);
    }

    public async Task AddAsync(
        UserEntity user,
        CancellationToken cancellationToken = default)
        => await _context.Set<UserEntity>()
            .AddAsync(user, cancellationToken);

    public Task UpdateAsync(
        UserEntity user,
        CancellationToken cancellationToken = default)
    {
        _context.Set<UserEntity>().Update(user);
        return Task.CompletedTask;
    }
}
