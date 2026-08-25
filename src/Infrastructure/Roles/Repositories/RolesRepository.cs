using Application.Roles.Abstraction;
using Domain.Roles.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RoleEntity = Domain.Roles.Entities.Roles;

namespace Infrastructure.Roles.Repository;

public sealed class RolesRepository : IRolesRepository
{
    private readonly VeterinaryDbContext _context;

    public RolesRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public Task<RoleEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<RoleEntity>()
            .FirstOrDefaultAsync(
                role => role.Id == id,
                cancellationToken);

    public async Task<IReadOnlyCollection<RoleEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<RoleEntity>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default,
        Guid? excludedId = null)
    {
        var roleName = RoleName.Create(name);

        return _context.Set<RoleEntity>()
            .AnyAsync(
                role => role.Name == roleName
                    && (!excludedId.HasValue || role.Id != excludedId.Value),
                cancellationToken);
    }

    public async Task AddAsync(
        RoleEntity role,
        CancellationToken cancellationToken = default)
        => await _context.Set<RoleEntity>()
            .AddAsync(role, cancellationToken);

    public Task UpdateAsync(
        RoleEntity role,
        CancellationToken cancellationToken = default)
    {
        _context.Set<RoleEntity>().Update(role);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        RoleEntity role,
        CancellationToken cancellationToken = default)
    {
        _context.Set<RoleEntity>().Remove(role);
        return Task.CompletedTask;
    }
}
