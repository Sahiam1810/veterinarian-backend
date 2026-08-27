using Domain.Availabilities.Entities;

namespace Application.Availabilities.Abstraction;

public interface IAvailabilityRepository
{
    Task<IReadOnlyCollection<Availability>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<Availability?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Availability>> GetAllByVeterinarianIdAsync(
        Guid veterinarianId,
        CancellationToken cancellationToken);

    Task<bool> ExistsOverlapAsync(
        Guid veterinarianId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        CancellationToken cancellationToken,
        Guid? excludedId = null);

    Task AddAsync(
        Availability availability,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        Availability availability,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Availability availability,
        CancellationToken cancellationToken);
}
