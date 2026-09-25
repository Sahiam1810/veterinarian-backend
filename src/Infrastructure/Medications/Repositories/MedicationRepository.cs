using Application.Medications.Abstraction;
using Domain.Medications.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Medications.Repositories;

public sealed class MedicationRepository(VeterinaryDbContext context) : IMedicationRepository
{
    public async Task<IEnumerable<Medication>> GetAllAsync(bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var query = context.Medications.AsQueryable();
        if (onlyActive)
        {
            query = query.Where(m => m.IsActive);
        }

        return await query.OrderBy(m => m.Name).ToListAsync(cancellationToken);
    }

    public async Task<Medication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Medications.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(
        string? code,
        CancellationToken cancellationToken = default,
        Guid? excludedId = null)
    {
        var normalizedCode = Medication.NormalizeCode(code);
        if (normalizedCode is null)
        {
            return false;
        }

        return await context.Medications.AnyAsync(
            medication => medication.Id != excludedId && medication.Code == normalizedCode,
            cancellationToken);
    }

    public async Task AddAsync(Medication medication, CancellationToken cancellationToken = default)
    {
        await context.Medications.AddAsync(medication, cancellationToken);
    }

    public Task UpdateAsync(Medication medication, CancellationToken cancellationToken = default)
    {
        context.Medications.Update(medication);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var medication = await GetByIdAsync(id, cancellationToken);
        if (medication is not null)
        {
            medication.Deactivate();
            context.Medications.Update(medication);
        }
    }
}
