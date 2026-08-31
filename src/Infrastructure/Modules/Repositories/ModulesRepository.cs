using Application.Modules.Abstraction;
using Domain.Modules.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ModuleEntity = Domain.Modules.Entities.ModuleEntity;

namespace Infrastructure.Modules.Repositories;

// Repositorio EF Core de módulos.
public sealed class ModulesRepository(VeterinaryDbContext context) : IModulesRepository
{
    public async Task<IReadOnlyCollection<ModuleEntity>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Set<ModuleEntity>()
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<ModuleEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Set<ModuleEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<ModuleEntity?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        var moduleName = ModuleName.Create(name);
        return context.Set<ModuleEntity>()
            .FirstOrDefaultAsync(x => x.Name == moduleName, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        var moduleName = ModuleName.Create(name);
        return await context.Set<ModuleEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.Name == moduleName, cancellationToken);
    }

    public async Task AddAsync(ModuleEntity module, CancellationToken cancellationToken) =>
        await context.Set<ModuleEntity>().AddAsync(module, cancellationToken);

    public Task UpdateAsync(ModuleEntity module, CancellationToken cancellationToken)
    {
        context.Set<ModuleEntity>().Update(module);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ModuleEntity module, CancellationToken cancellationToken)
    {
        context.Set<ModuleEntity>().Remove(module);
        return Task.CompletedTask;
    }
}
