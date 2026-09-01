using Application.UserPermissions.Abstraction;
using Domain.Modules.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ModuleEntity = Domain.Modules.Entities.ModuleEntity;
using UserPermissionEntity = Domain.UserPermissions.Entities.UserPermission;

namespace Infrastructure.UserPermissions.Repositories;

// Repositorio EF Core de permisos puntuales por usuario y módulo.
public sealed class UserPermissionsRepository(VeterinaryDbContext context) : IUserPermissionsRepository
{
    public async Task<IReadOnlyCollection<UserPermissionEntity>> GetAllAsync(
        CancellationToken cancellationToken) =>
        await context.Set<UserPermissionEntity>()
            .AsNoTracking()
            .OrderBy(x => x.UserId)
            .ThenBy(x => x.ModuleId)
            .ToListAsync(cancellationToken);

    public Task<UserPermissionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Set<UserPermissionEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<UserPermissionEntity>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await context.Set<UserPermissionEntity>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.ModuleId)
            .ToListAsync(cancellationToken);

    public Task<UserPermissionEntity?> GetByUserAndModuleIdAsync(
        Guid userId,
        Guid moduleId,
        CancellationToken cancellationToken) =>
        context.Set<UserPermissionEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.ModuleId == moduleId,
                cancellationToken);

    public async Task<UserPermissionEntity?> GetByUserAndModuleNameAsync(
        Guid userId,
        string moduleName,
        CancellationToken cancellationToken)
    {
        var name = ModuleName.Create(moduleName);

        var module = await context.Set<ModuleEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Name == name, cancellationToken);

        if (module is null)
        {
            return null;
        }

        return await context.Set<UserPermissionEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.UserId == userId && p.ModuleId == module.Id,
                cancellationToken);
    }

    public async Task AddAsync(UserPermissionEntity permission, CancellationToken cancellationToken) =>
        await context.Set<UserPermissionEntity>().AddAsync(permission, cancellationToken);

    public Task UpdateAsync(UserPermissionEntity permission, CancellationToken cancellationToken)
    {
        context.Set<UserPermissionEntity>().Update(permission);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(UserPermissionEntity permission, CancellationToken cancellationToken)
    {
        context.Set<UserPermissionEntity>().Remove(permission);
        return Task.CompletedTask;
    }
}
