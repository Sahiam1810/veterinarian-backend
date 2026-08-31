using Application.Modules.Abstraction;
using Domain.Modules.Entities;
using Domain.Modules.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Repository;

public sealed class ModuleRepository(VeterinaryDbContext context) : IModuleRepository
{
    public async Task AddAsync(
        ModuleEntity module,
        CancellationToken cancellationToken)
        => await context.Modules.AddAsync(module, cancellationToken);

    public Task<ModuleEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
        => context.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(module => module.Id == id, cancellationToken);

    public Task<ModuleEntity?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var moduleName = ModuleName.Create(name);

        return context.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(module => module.Name == moduleName, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ModuleEntity>> GetAllAsync(
        CancellationToken cancellationToken)
        => await context.Modules
            .AsNoTracking()
            .OrderBy(module => module.Name)
            .ToListAsync(cancellationToken);

    public Task UpdateAsync(
        ModuleEntity module,
        CancellationToken cancellationToken)
    {
        context.Modules.Update(module);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        ModuleEntity module,
        CancellationToken cancellationToken)
    {
        context.Modules.Remove(module);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken,
        Guid? excludedId = null)
    {
        var moduleName = ModuleName.Create(name);

        return context.Modules.AnyAsync(
            module => module.Name == moduleName
                && (!excludedId.HasValue || module.Id != excludedId.Value),
            cancellationToken);
    }
}
