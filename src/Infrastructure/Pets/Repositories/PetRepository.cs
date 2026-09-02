using Application.Pets.Abstraction;
using Domain.Pets.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Pets.Repositories;

public sealed class PetRepository : IPetRepository
{
    private readonly VeterinaryDbContext _context;

    public PetRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<PetEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<PetEntity>().ToListAsync(cancellationToken);
    }

    public async Task<PetEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Set<PetEntity>()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PetEntity>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        return await _context.Set<PetEntity>()
            .Include(p => p.Species)
            .Include(p => p.Race)
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PetEntity pet, CancellationToken cancellationToken)
    {
        await _context.Set<PetEntity>().AddAsync(pet, cancellationToken);
    }

    public Task UpdateAsync(PetEntity pet, CancellationToken cancellationToken)
    {
        _context.Set<PetEntity>().Update(pet);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(PetEntity pet, CancellationToken cancellationToken)
    {
        _context.Set<PetEntity>().Remove(pet);
        return Task.CompletedTask;
    }
}
