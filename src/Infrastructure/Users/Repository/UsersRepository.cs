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

    public Task<UserEntity?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var userEmail = UserEmail.Create(email);

        return _context.Set<UserEntity>()
            .FirstOrDefaultAsync(
                user => user.Email == userEmail,
                cancellationToken);
    }

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

    public Task DeleteAsync(
        UserEntity user,
        CancellationToken cancellationToken = default)
    {
        _context.Set<UserEntity>().Remove(user);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<UserEntity>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return Array.Empty<UserEntity>();
        }

        return await _context.Set<UserEntity>()
            .AsNoTracking()
            .Where(u => idList.Contains(u.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<(UserEntity User, string RoleName)>> GetStaffUsersAsync(
        CancellationToken cancellationToken = default)
    {
        // Se compara contra el value object (igual que RolesRepository.GetByNameAsync):
        // acceder a r.Name.Value dentro del Where no se traduce a SQL con Oracle.
        var allowedRoles = new[] { "Veterinario", "Auxiliar", "Administrador" }
            .Select(Domain.Roles.ValueObjects.RoleName.Create)
            .ToArray();

        var query = from u in _context.Set<UserEntity>().AsNoTracking()
                    join r in _context.Set<Domain.Roles.Entities.Roles>().AsNoTracking() on u.RoleId equals r.Id
                    where u.IsActive && allowedRoles.Contains(r.Name)
                    orderby u.FullName
                    select new { User = u, Role = r };

        var results = await query.ToListAsync(cancellationToken);
        return results.Select(x => (x.User, x.Role.Name.Value)).ToList();
    }
}
