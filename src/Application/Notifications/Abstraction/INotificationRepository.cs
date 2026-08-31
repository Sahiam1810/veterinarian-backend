using Domain.Notifications.Entities;

namespace Application.Notifications.Abstraction;

public interface INotificationRepository
{
    Task<IReadOnlyCollection<Notification>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Notification?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Notification>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Notification>> GetByAppointmentIdAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Guid>> GetNotifiedAppointmentIdsAsync(
        IReadOnlyCollection<Guid> appointmentIds,
        string type,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Notification notification,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Notification notification,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Notification notification,
        CancellationToken cancellationToken = default);
}
