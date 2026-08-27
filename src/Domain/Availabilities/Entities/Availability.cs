using Domain.Availabilities.ValueObjects;
using Domain.Common;
using Domain.Veterinarians.Entities;

namespace Domain.Availabilities.Entities;

public sealed class Availability : BaseEntity<Guid>
{
    private Availability()
    {
    }

    public Availability(
        Guid veterinarianId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        bool isActive = true)
    {
        var timeRange = TimeRange.Create(startTime, endTime);

        Id = Guid.NewGuid();
        VeterinarianId = veterinarianId;
        DayOfWeek = dayOfWeek;
        StartTime = timeRange.StartTime;
        EndTime = timeRange.EndTime;
        IsActive = isActive;
    }

    public Guid VeterinarianId { get; private set; }
    public Veterinarian? Veterinarian { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        Guid veterinarianId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        bool isActive)
    {
        var timeRange = TimeRange.Create(startTime, endTime);

        VeterinarianId = veterinarianId;
        DayOfWeek = dayOfWeek;
        StartTime = timeRange.StartTime;
        EndTime = timeRange.EndTime;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
