namespace Api.Availabilities.Dtos;

public sealed record UpdateAvailabilityRequest(
    Guid VeterinarianId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive);
