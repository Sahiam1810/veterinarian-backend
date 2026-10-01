using Application.HospitalizationStays.Abstraction;
using Domain.HospitalizationStays.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.HospitalizationStays.Repositories;

public sealed class HospitalizationNoteRepository : IHospitalizationNoteRepository
{
    private readonly VeterinaryDbContext _context;

    public HospitalizationNoteRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<HospitalizationNote>> GetByStayIdAsync(Guid stayId, CancellationToken cancellationToken = default)
        => await _context.Set<HospitalizationNote>()
            .AsNoTracking()
            .Where(x => x.StayId == stayId)
            .OrderBy(x => x.FechaHora)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(HospitalizationNote note, CancellationToken cancellationToken = default)
        => await _context.Set<HospitalizationNote>().AddAsync(note, cancellationToken);
}
