using Domain.ProcedureOrders.Entities;
using Application.Common.Models;

namespace Application.ProcedureOrders.Abstraction;

public interface IProcedureOrderRepository
{
    Task<ProcedureOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcedureOrder>> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcedureOrder>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ProcedureOrder>> GetByHospitalizationStayAsync(Guid hospitalizationStayId, Guid? appointmentId, CancellationToken cancellationToken = default);
    Task<PaginatedResult<ProcedureOrder>> GetClinicalResultsAsync(
        string? search,
        Guid? veterinarianId,
        DateTime? fromUtc,
        DateTime? toUtc,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(ProcedureOrder procedureOrder, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProcedureOrder procedureOrder, CancellationToken cancellationToken = default);
}
