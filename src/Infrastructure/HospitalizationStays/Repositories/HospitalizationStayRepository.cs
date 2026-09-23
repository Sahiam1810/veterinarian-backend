using Application.HospitalizationStays.Abstraction;
using Domain.HospitalizationStays.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.HospitalizationStays.Repositories;

public sealed class HospitalizationStayRepository : IHospitalizationStayRepository
{
    private readonly VeterinaryDbContext _context;

    public HospitalizationStayRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public Task<HospitalizationStay?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Set<HospitalizationStay>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<HospitalizationStay?> GetActiveByPetIdAsync(Guid clientPetId, CancellationToken cancellationToken = default)
        => await _context.Set<HospitalizationStay>()
            .FirstOrDefaultAsync(x => x.ClientPetId == clientPetId && x.Estado == HospitalizationStayStatus.Activa, cancellationToken);

    public async Task<IReadOnlyCollection<HospitalizationStay>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await _context.Set<HospitalizationStay>()
            .AsNoTracking()
            .Where(x => x.Estado == HospitalizationStayStatus.Activa)
            .OrderBy(x => x.FechaIngreso)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<HospitalizationStay>> GetByClientPetIdAsync(Guid clientPetId, CancellationToken cancellationToken = default)
        => await _context.Set<HospitalizationStay>()
            .AsNoTracking()
            .Where(x => x.ClientPetId == clientPetId)
            .OrderByDescending(x => x.FechaIngreso)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(HospitalizationStay stay, CancellationToken cancellationToken = default)
        => await _context.Set<HospitalizationStay>().AddAsync(stay, cancellationToken);

    public Task UpdateAsync(HospitalizationStay stay, CancellationToken cancellationToken = default)
    {
        _context.Set<HospitalizationStay>().Update(stay);
        return Task.CompletedTask;
    }
}
