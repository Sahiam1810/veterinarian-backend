using Application.RolePermissions.Abstraction;
using Domain.Modules.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ModuleEntity = Domain.Modules.Entities.ModuleEntity;
using RolePermissionEntity = Domain.RolePermissions.Entities.RolePermission;

namespace Infrastructure.RolePermissions.Repositories;

public sealed class RolePermissionsRepository : IRolePermissionsRepository
{
    private readonly VeterinaryDbContext _context;

    public RolePermissionsRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<RolePermissionEntity?> GetByRoleAndModuleNameAsync(
        Guid roleId,
        string moduleName,
        CancellationToken cancellationToken)
    {
        var name = ModuleName.Create(moduleName);

        var module = await _context.Set<ModuleEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.Name == name,
                cancellationToken);

        if (module is null)
        {
            return null;
        }

        return await _context.Set<RolePermissionEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.RoleId == roleId && p.ModuleId == module.Id,
                cancellationToken);
    }
}
