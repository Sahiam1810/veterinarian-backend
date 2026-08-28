using Application.Vaccinations.Abstraction;
using Domain.Vaccinations.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Vaccinations.Repositories;

public sealed class VaccinationRepository : IVaccinationRepository
{
    private readonly VeterinaryDbContext _context;

    public VaccinationRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<Vaccination>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<Vaccination>()
            .Include(x => x.ClientPet)
            .Include(x => x.Record)
            .AsNoTracking()
            .OrderByDescending(x => x.ApplicationDate)
            .ToListAsync(cancellationToken);

    public Task<Vaccination?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<Vaccination>()
            .Include(x => x.ClientPet)
            .Include(x => x.Record)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(
        Vaccination vaccination,
        CancellationToken cancellationToken = default)
        => await _context.Set<Vaccination>()
            .AddAsync(vaccination, cancellationToken);

    public Task UpdateAsync(
        Vaccination vaccination,
        CancellationToken cancellationToken = default)
    {
        _context.Set<Vaccination>().Update(vaccination);
        return Task.CompletedTask;
    }
}
