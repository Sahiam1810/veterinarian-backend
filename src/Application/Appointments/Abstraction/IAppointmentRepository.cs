using Domain.Appointments.Entities;

namespace Application.Appointments.Abstraction;

public interface IAppointmentRepository
{
    Task<IReadOnlyCollection<Appointment>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<Appointment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task AddAsync(
        Appointment appointment,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        Appointment appointment,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Appointment appointment,
        CancellationToken cancellationToken);
}
