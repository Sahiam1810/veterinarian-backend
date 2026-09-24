using Domain.ProcedureOrders.Entities;

namespace Application.ProcedureOrders.Abstraction;

public interface IProcedureOrderRepository
{
    Task<ProcedureOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcedureOrder>> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcedureOrder>> GetByHospitalizationStayIdAsync(Guid hospitalizationStayId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcedureOrder>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ProcedureOrder procedureOrder, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProcedureOrder procedureOrder, CancellationToken cancellationToken = default);
}
