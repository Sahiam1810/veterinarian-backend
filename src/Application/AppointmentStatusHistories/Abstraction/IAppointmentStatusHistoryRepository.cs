using Domain.AppointmentStatusHistories.Entities;

namespace Application.AppointmentStatusHistories.Abstraction;

public interface IAppointmentStatusHistoryRepository
{
    Task<IReadOnlyCollection<AppointmentStatusHistory>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<AppointmentStatusHistory?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    // Ordenado por CreatedAt descendente: el primero es el vigente de la cita.
    Task<IReadOnlyCollection<AppointmentStatusHistory>> GetByAppointmentIdAsync(
        Guid appointmentId,
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
