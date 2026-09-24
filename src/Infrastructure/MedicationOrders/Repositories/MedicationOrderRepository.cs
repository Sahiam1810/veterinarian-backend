using Application.MedicationOrders.Abstraction;
using Domain.MedicationOrders.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.MedicationOrders.Repositories;

public sealed class MedicationOrderRepository(VeterinaryDbContext context) : IMedicationOrderRepository
{
    public async Task<MedicationOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.MedicationOrders
            .Include(o => o.Items)
                .ThenInclude(i => i.Medication)
            .Include(o => o.ClientPet)
            .Include(o => o.Veterinarian)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<MedicationOrder>> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        return await context.MedicationOrders
            .Include(o => o.Items)
                .ThenInclude(i => i.Medication)
            .Include(o => o.ClientPet)
            .Include(o => o.Veterinarian)
            .Where(o => o.AppointmentId == appointmentId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MedicationOrder>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await context.MedicationOrders
            .Include(o => o.Items)
            .Include(o => o.ClientPet!)
                .ThenInclude(cp => cp.Pet)
            .Include(o => o.ClientPet!)
                .ThenInclude(cp => cp.Client)
            .Where(o => o.Status == "Pendiente")
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MedicationOrder>> GetByHospitalizationStayAsync(
        Guid? appointmentId,
        Guid clientPetId,
        DateTime admittedAt,
        DateTime? dischargedAt,
        CancellationToken cancellationToken = default)
    {
        var query = context.MedicationOrders
            .Include(o => o.Items)
                .ThenInclude(i => i.Medication)
            .Include(o => o.ClientPet)
            .Include(o => o.Veterinarian)
            .AsQueryable();

        if (appointmentId.HasValue)
        {
            query = query.Where(o => o.AppointmentId == appointmentId.Value ||
                (o.ClientPetId == clientPetId && o.CreatedAt >= admittedAt && (dischargedAt == null || o.CreatedAt <= dischargedAt.Value)));
        }
        else
        {
            query = query.Where(o => o.ClientPetId == clientPetId && o.CreatedAt >= admittedAt && (dischargedAt == null || o.CreatedAt <= dischargedAt.Value));
        }

        return await query.OrderByDescending(o => o.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(MedicationOrder medicationOrder, CancellationToken cancellationToken = default)
    {
        await context.MedicationOrders.AddAsync(medicationOrder, cancellationToken);
    }

    public Task UpdateAsync(MedicationOrder medicationOrder, CancellationToken cancellationToken = default)
    {
        context.MedicationOrders.Update(medicationOrder);
        return Task.CompletedTask;
    }
}
