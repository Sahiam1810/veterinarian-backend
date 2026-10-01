using Application.VeterinarianAbsences.Abstraction;
using Domain.VeterinarianAbsences.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.VeterinarianAbsences.Repositories;

public sealed class VeterinarianAbsenceRepository : IVeterinarianAbsenceRepository
{
    private readonly VeterinaryDbContext _context;

    public VeterinarianAbsenceRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public Task<VeterinarianAbsence?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
        => _context.Set<VeterinarianAbsence>()
            .Include(x => x.Veterinarian)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<VeterinarianAbsence>> GetAllAsync(
        CancellationToken cancellationToken)
        => await _context.Set<VeterinarianAbsence>()
            .Include(x => x.Veterinarian)
            .AsNoTracking()
            .OrderBy(x => x.StartAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<VeterinarianAbsence>> GetAllByVeterinarianIdAsync(
        Guid veterinarianId,
        CancellationToken cancellationToken)
        => await _context.Set<VeterinarianAbsence>()
            .AsNoTracking()
            .Where(x => x.VeterinarianId == veterinarianId)
            .OrderBy(x => x.StartAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<VeterinarianAbsence>> GetOverlappingAsync(
        Guid veterinarianId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
        => await _context.Set<VeterinarianAbsence>()
            .AsNoTracking()
            .Where(x => x.VeterinarianId == veterinarianId
                && x.StartAtUtc < toUtc
                && x.EndAtUtc > fromUtc)
            .OrderBy(x => x.StartAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        VeterinarianAbsence absence,
        CancellationToken cancellationToken)
        => await _context.Set<VeterinarianAbsence>()
            .AddAsync(absence, cancellationToken);

    public Task UpdateAsync(
        VeterinarianAbsence absence,
        CancellationToken cancellationToken)
    {
        _context.Set<VeterinarianAbsence>().Update(absence);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        VeterinarianAbsence absence,
        CancellationToken cancellationToken)
    {
        _context.Set<VeterinarianAbsence>().Remove(absence);
        return Task.CompletedTask;
    }
}
