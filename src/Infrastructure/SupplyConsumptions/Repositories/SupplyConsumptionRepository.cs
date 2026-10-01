using Application.SupplyConsumptions.Abstraction;
using Domain.SupplyConsumptions.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SupplyConsumptions.Repositories;

public sealed class SupplyConsumptionRepository(VeterinaryDbContext context) : ISupplyConsumptionRepository
{
    public async Task<IEnumerable<SupplyConsumption>> GetByHospitalizationStayIdAsync(Guid stayId, CancellationToken cancellationToken = default)
    {
        return await context.SupplyConsumptions
            .Where(sc => sc.HospitalizationStayId == stayId)
            .OrderByDescending(sc => sc.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplyConsumption?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.SupplyConsumptions.FirstOrDefaultAsync(sc => sc.Id == id, cancellationToken);
    }

    public async Task AddAsync(SupplyConsumption consumption, CancellationToken cancellationToken = default)
    {
        await context.SupplyConsumptions.AddAsync(consumption, cancellationToken);
    }

    public async Task<decimal> GetTotalByHospitalizationStayIdAsync(Guid stayId, CancellationToken cancellationToken = default)
    {
        return await context.SupplyConsumptions
            .Where(sc => sc.HospitalizationStayId == stayId)
            .SumAsync(sc => sc.Total, cancellationToken);
    }
}
