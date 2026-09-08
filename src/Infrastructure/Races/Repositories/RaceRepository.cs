using Application.Races.Abstraction;
using Domain.Races.Entities;
using Domain.Races.ValueObjects;
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

    public async Task<IReadOnlyCollection<RaceEntity>> GetAllAsync(
        Guid? speciesId,
        CancellationToken cancellationToken)
    {
        var query = _context.Set<RaceEntity>().AsNoTracking();
        if (speciesId.HasValue)
        {
            query = query.Where(race => race.SpeciesId == speciesId.Value);
        }

        return await query
            .OrderBy(race => race.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<RaceEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Set<RaceEntity>()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        Guid speciesId,
        CancellationToken cancellationToken,
        Guid? excludedId = null)
    {
        var nameVo = RaceName.Create(name);
        var query = _context.Set<RaceEntity>()
            .Where(race => race.SpeciesId == speciesId && race.Name == nameVo);
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
