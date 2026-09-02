using Application.MedicalRecords.Abstraction;
using Domain.MedicalRecords.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.MedicalRecords.Repositories;

public sealed class MedicalRecordRepository : IMedicalRecordRepository
{
    private readonly VeterinaryDbContext _context;

    public MedicalRecordRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<MedicalRecord>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<MedicalRecord>()
            .Include(x => x.ClientPet)
            .Include(x => x.Appointment)
            .Include(x => x.Diagnostic)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<MedicalRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<MedicalRecord>()
            .Include(x => x.ClientPet)
            .Include(x => x.Appointment)
            .Include(x => x.Diagnostic)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<MedicalRecord>> GetByClientPetIdsAsync(
        IReadOnlyCollection<Guid> clientPetIds,
        CancellationToken cancellationToken = default)
        => await _context.Set<MedicalRecord>()
            .Include(x => x.ClientPet)
            .Include(x => x.Appointment)
            .Include(x => x.Diagnostic)
            .Where(x => clientPetIds.Contains(x.ClientPetId))
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        MedicalRecord medicalRecord,
        CancellationToken cancellationToken = default)
        => await _context.Set<MedicalRecord>()
            .AddAsync(medicalRecord, cancellationToken);

    public Task<bool> ExistsByAppointmentIdAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
        => _context.Set<MedicalRecord>()
            .AsNoTracking()
            .AnyAsync(x => x.AppointmentId == appointmentId, cancellationToken);
}
