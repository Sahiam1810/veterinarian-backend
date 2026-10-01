using Application.ProcedureOrders.Abstraction;
using Application.Common.Models;
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
                .ThenInclude(i => i.Procedure)
            .Include(o => o.ClientPet!)
                .ThenInclude(cp => cp.Pet)
            .Include(o => o.ClientPet!)
                .ThenInclude(cp => cp.Client)
            .Where(o => o.Status == "Pendiente")
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProcedureOrder>> GetByHospitalizationStayAsync(
        Guid hospitalizationStayId,
        Guid? appointmentId,
        CancellationToken cancellationToken = default)
    {
        var query = context.ProcedureOrders
            .Include(o => o.Items)
                .ThenInclude(i => i.Procedure)
            .Include(o => o.ClientPet)
            .Include(o => o.Veterinarian)
            .AsQueryable();

        query = query.Where(o => o.HospitalizationStayId == hospitalizationStayId ||
            (appointmentId.HasValue && o.AppointmentId == appointmentId.Value));

        return await query.OrderByDescending(o => o.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<PaginatedResult<ProcedureOrder>> GetClinicalResultsAsync(
        string? search,
        Guid? veterinarianId,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.ProcedureOrders
            .Include(o => o.Items)
                .ThenInclude(i => i.Procedure)
            .Include(o => o.ClientPet!)
                .ThenInclude(cp => cp.Pet)
                    .ThenInclude(p => p.Species)
            .Include(o => o.ClientPet!)
                .ThenInclude(cp => cp.Pet)
                    .ThenInclude(p => p.Race)
            .Include(o => o.ClientPet!)
                .ThenInclude(cp => cp.Client)
            .Include(o => o.Veterinarian!)
                .ThenInclude(v => v.User)
            .Where(o => o.ResultFileUrl != null && o.ResultFileUrl != "");

        if (veterinarianId.HasValue)
        {
            query = query.Where(o => o.VeterinarianId == veterinarianId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            if (string.Equals(normalizedStatus, "COMPLETED_WITH_RESULT", StringComparison.OrdinalIgnoreCase))
            {
                normalizedStatus = ProcedureOrder.CompletedStatus;
            }

            query = query.Where(o => o.Status == normalizedStatus);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(o => o.UpdatedAt >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(o => o.UpdatedAt <= toUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToUpper();
            query = query.Where(o =>
                o.ClientPet!.Pet.Name.Value.ToUpper().Contains(searchTerm)
                || o.ClientPet.Client.FullName.Value.ToUpper().Contains(searchTerm)
                || o.Items.Any(i => i.Procedure!.Name.ToUpper().Contains(searchTerm)));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(o => o.UpdatedAt)
            .ThenByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ProcedureOrder>(
            items,
            new PaginationMetadata(page, pageSize, totalItems, totalPages));
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
