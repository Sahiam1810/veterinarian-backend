using Domain.SupplyConsumptions.Entities;

namespace Application.SupplyConsumptions.Abstraction;

public interface ISupplyConsumptionRepository
{
    Task<IEnumerable<SupplyConsumption>> GetByHospitalizationStayIdAsync(Guid stayId, CancellationToken cancellationToken = default);
    Task<SupplyConsumption?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(SupplyConsumption consumption, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalByHospitalizationStayIdAsync(Guid stayId, CancellationToken cancellationToken = default);
}
