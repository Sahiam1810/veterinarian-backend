using Application.Priorities.Abstraction;
using Domain.Priorities.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Priorities.Repositories;

// Repositorio EF Core de prioridades.
public sealed class PriorityRepository(VeterinaryDbContext context) : IPriorityRepository
{
    public async Task<IReadOnlyCollection<PriorityEntity>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Set<PriorityEntity>()
            .AsNoTracking()
            .OrderBy(x => x.Name.Value)
            .ToListAsync(cancellationToken);

    public Task<PriorityEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Set<PriorityEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(PriorityEntity priority, CancellationToken cancellationToken) =>
        await context.Set<PriorityEntity>().AddAsync(priority, cancellationToken);

    public Task UpdateAsync(PriorityEntity priority, CancellationToken cancellationToken)
    {
        context.Set<PriorityEntity>().Update(priority);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(PriorityEntity priority, CancellationToken cancellationToken)
    {
        context.Set<PriorityEntity>().Remove(priority);
        return Task.CompletedTask;
    }
}
