namespace Domain.Availabilities.ValueObjects;

public sealed record TimeRange
{
    private TimeRange(TimeOnly startTime, TimeOnly endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }

    public TimeOnly StartTime { get; }

    public TimeOnly EndTime { get; }

    public static TimeRange Create(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
        {
            throw new ArgumentException(
                "La hora de fin debe ser posterior a la hora de inicio.",
                nameof(endTime));
        }

        return new TimeRange(startTime, endTime);
    }
}
