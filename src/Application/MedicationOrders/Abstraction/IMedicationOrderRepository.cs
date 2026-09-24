using Domain.MedicationOrders.Entities;

namespace Application.MedicationOrders.Abstraction;

public interface IMedicationOrderRepository
{
    Task<MedicationOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<MedicationOrder>> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MedicationOrder>> GetByHospitalizationStayIdAsync(Guid hospitalizationStayId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MedicationOrder>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task AddAsync(MedicationOrder medicationOrder, CancellationToken cancellationToken = default);
    Task UpdateAsync(MedicationOrder medicationOrder, CancellationToken cancellationToken = default);
}
