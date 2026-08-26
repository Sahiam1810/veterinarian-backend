using Domain.StatusAppointments.Entities;

namespace Application.StatusAppointments.Abstraction;

public interface IStatusAppointmentRepository
{
    Task<IReadOnlyCollection<StatusAppointment>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<StatusAppointment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken,
        Guid? excludedId = null);

    Task AddAsync(
        StatusAppointment statusAppointment,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        StatusAppointment statusAppointment,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        StatusAppointment statusAppointment,
        CancellationToken cancellationToken);
}
