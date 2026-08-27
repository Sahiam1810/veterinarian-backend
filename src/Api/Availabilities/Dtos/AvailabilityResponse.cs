namespace Api.Availabilities.Dtos;

public sealed record AvailabilityResponse(
    Guid Id,
    Guid VeterinarianId,
    string? VeterinarianLicenseNumber,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive,
    DateTime CreatedAt);
