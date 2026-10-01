using Application.Supplies.Abstraction;
using Domain.Supplies.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Supplies.Repositories;

public sealed class SupplyRepository(VeterinaryDbContext context) : ISupplyRepository
{
    public async Task<IEnumerable<Supply>> GetAllAsync(bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var query = context.Supplies.AsQueryable();
        if (onlyActive)
        {
            query = query.Where(s => s.IsActive);
        }

        return await query.OrderBy(s => s.Name).ToListAsync(cancellationToken);
    }

    public async Task<Supply?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Supplies.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddAsync(Supply supply, CancellationToken cancellationToken = default)
    {
        await context.Supplies.AddAsync(supply, cancellationToken);
    }

    public Task UpdateAsync(Supply supply, CancellationToken cancellationToken = default)
    {
        context.Supplies.Update(supply);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supply = await GetByIdAsync(id, cancellationToken);
        if (supply is not null)
        {
            supply.Deactivate();
            context.Supplies.Update(supply);
        }
    }
}
