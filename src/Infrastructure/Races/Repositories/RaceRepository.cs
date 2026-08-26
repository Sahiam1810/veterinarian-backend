using Application.Races.Abstraction;
using Domain.Races.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Races.Repositories;

public sealed class RaceRepository : IRaceRepository
{
    private readonly VeterinaryDbContext _context;

    public RaceRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<RaceEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<RaceEntity>().ToListAsync(cancellationToken);
    }

    public async Task<RaceEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Set<RaceEntity>()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken, Guid? excludedId = null)
    {
        var query = _context.Set<RaceEntity>().Where(r => r.Name.Value == name);
        if (excludedId.HasValue)
        {
            query = query.Where(r => r.Id != excludedId.Value);
        }
        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(RaceEntity race, CancellationToken cancellationToken)
    {
        await _context.Set<RaceEntity>().AddAsync(race, cancellationToken);
    }

    public Task UpdateAsync(RaceEntity race, CancellationToken cancellationToken)
    {
        _context.Set<RaceEntity>().Update(race);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(RaceEntity race, CancellationToken cancellationToken)
    {
        _context.Set<RaceEntity>().Remove(race);
        return Task.CompletedTask;
    }
}
