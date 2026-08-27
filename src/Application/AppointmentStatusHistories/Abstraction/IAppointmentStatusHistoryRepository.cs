using Domain.AppointmentStatusHistories.Entities;

namespace Application.AppointmentStatusHistories.Abstraction;

public interface IAppointmentStatusHistoryRepository
{
    Task<IReadOnlyCollection<AppointmentStatusHistory>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<AppointmentStatusHistory?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task AddAsync(
        AppointmentStatusHistory appointmentStatusHistory,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        AppointmentStatusHistory appointmentStatusHistory,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        AppointmentStatusHistory appointmentStatusHistory,
        CancellationToken cancellationToken);
}
