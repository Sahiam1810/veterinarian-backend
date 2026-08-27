namespace Api.Availabilities.Dtos;

public sealed record CreateAvailabilityRequest(
    Guid VeterinarianId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive = true);
