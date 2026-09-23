using Application.ProcedureOrders.Abstraction;
using Domain.ProcedureOrders.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ProcedureOrders.Repositories;

public sealed class ProcedureOrderRepository(VeterinaryDbContext context) : IProcedureOrderRepository
{
    public async Task<ProcedureOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.ProcedureOrders
            .Include(o => o.Items)
                .ThenInclude(i => i.Procedure)
            .Include(o => o.ClientPet)
            .Include(o => o.Veterinarian)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ProcedureOrder>> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        return await context.ProcedureOrders
            .Include(o => o.Items)
                .ThenInclude(i => i.Procedure)
            .Include(o => o.ClientPet)
            .Include(o => o.Veterinarian)
            .Where(o => o.AppointmentId == appointmentId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProcedureOrder>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await context.ProcedureOrders
            .Include(o => o.Items)
            .Include(o => o.ClientPet!)
                .ThenInclude(cp => cp.Pet)
            .Include(o => o.ClientPet!)
                .ThenInclude(cp => cp.Client)
            .Where(o => o.Status == "Pendiente")
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProcedureOrder procedureOrder, CancellationToken cancellationToken = default)
    {
        await context.ProcedureOrders.AddAsync(procedureOrder, cancellationToken);
    }

    public Task UpdateAsync(ProcedureOrder procedureOrder, CancellationToken cancellationToken = default)
    {
        context.ProcedureOrders.Update(procedureOrder);
        return Task.CompletedTask;
    }
}
