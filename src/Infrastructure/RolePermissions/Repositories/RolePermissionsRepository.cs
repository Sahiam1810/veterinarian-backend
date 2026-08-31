using Application.RolePermissions.Abstraction;
using Domain.Modules.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ModuleEntity = Domain.Modules.Entities.ModuleEntity;
using RolePermissionEntity = Domain.RolePermissions.Entities.RolePermission;

namespace Infrastructure.RolePermissions.Repositories;

// Repositorio EF Core de permisos por rol y módulo.
public sealed class RolePermissionsRepository(VeterinaryDbContext context) : IRolePermissionsRepository
{
    public async Task<IReadOnlyCollection<RolePermissionEntity>> GetAllAsync(
        CancellationToken cancellationToken) =>
        await context.Set<RolePermissionEntity>()
            .AsNoTracking()
            .OrderBy(x => x.RoleId)
            .ThenBy(x => x.ModuleId)
            .ToListAsync(cancellationToken);

    public Task<RolePermissionEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Set<RolePermissionEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<RolePermissionEntity>> GetByRoleIdAsync(
        Guid roleId,
        CancellationToken cancellationToken) =>
        await context.Set<RolePermissionEntity>()
            .AsNoTracking()
            .Where(x => x.RoleId == roleId)
            .OrderBy(x => x.ModuleId)
            .ToListAsync(cancellationToken);

    public Task<RolePermissionEntity?> GetByRoleAndModuleIdAsync(
        Guid roleId,
        Guid moduleId,
        CancellationToken cancellationToken) =>
        context.Set<RolePermissionEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.RoleId == roleId && x.ModuleId == moduleId,
                cancellationToken);

    public async Task<RolePermissionEntity?> GetByRoleAndModuleNameAsync(
        Guid roleId,
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

        return await context.Set<RolePermissionEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.RoleId == roleId && p.ModuleId == module.Id,
                cancellationToken);
    }

    public async Task AddAsync(RolePermissionEntity permission, CancellationToken cancellationToken) =>
        await context.Set<RolePermissionEntity>().AddAsync(permission, cancellationToken);

    public Task UpdateAsync(RolePermissionEntity permission, CancellationToken cancellationToken)
    {
        context.Set<RolePermissionEntity>().Update(permission);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(RolePermissionEntity permission, CancellationToken cancellationToken)
    {
        context.Set<RolePermissionEntity>().Remove(permission);
        return Task.CompletedTask;
    }
}
