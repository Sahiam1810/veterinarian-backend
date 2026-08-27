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

    public async Task AddAsync(
        MedicalRecord medicalRecord,
        CancellationToken cancellationToken = default)
        => await _context.Set<MedicalRecord>()
            .AddAsync(medicalRecord, cancellationToken);

    public Task UpdateAsync(
        MedicalRecord medicalRecord,
        CancellationToken cancellationToken = default)
    {
        _context.Set<MedicalRecord>().Update(medicalRecord);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        MedicalRecord medicalRecord,
        CancellationToken cancellationToken = default)
    {
        _context.Set<MedicalRecord>().Remove(medicalRecord);
        return Task.CompletedTask;
    }
}
