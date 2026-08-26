using Application.Species.Abstraction;
using Domain.Species.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Species.Repositories;

public sealed class SpeciesRepository : ISpeciesRepository
{
    private readonly VeterinaryDbContext _context;

    public SpeciesRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<SpeciesEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<SpeciesEntity>().ToListAsync(cancellationToken);
    }

    public async Task<SpeciesEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Set<SpeciesEntity>()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken, Guid? excludedId = null)
    {
        var query = _context.Set<SpeciesEntity>().Where(s => s.Name.Value == name);
        if (excludedId.HasValue)
        {
            query = query.Where(s => s.Id != excludedId.Value);
        }
        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(SpeciesEntity species, CancellationToken cancellationToken)
    {
        await _context.Set<SpeciesEntity>().AddAsync(species, cancellationToken);
    }

    public Task UpdateAsync(SpeciesEntity species, CancellationToken cancellationToken)
    {
        _context.Set<SpeciesEntity>().Update(species);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(SpeciesEntity species, CancellationToken cancellationToken)
    {
        _context.Set<SpeciesEntity>().Remove(species);
        return Task.CompletedTask;
    }
}
