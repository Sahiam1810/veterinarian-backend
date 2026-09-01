using Domain.Appointments.Entities;

namespace Application.Appointments.Abstraction;

public interface IAppointmentRepository
{
    Task<IReadOnlyCollection<Appointment>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<Appointment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Appointment>> GetByClientPetIdsAsync(
        IReadOnlyCollection<Guid> clientPetIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Appointment>> GetByVeterinarianIdAsync(
        Guid veterinarianId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Appointment>> GetScheduledBetweenAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingAppointmentAsync(
        Guid clientPetId,
        Guid veterinarianId,
        DateTime start,
        DateTime end,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default);

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
