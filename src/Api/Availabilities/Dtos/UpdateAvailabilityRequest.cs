namespace Api.Availabilities.Dtos;

public sealed record UpdateAvailabilityRequest(
    Guid VeterinarianId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive,
    int SlotDurationMinutes = 30,
    string? ShiftName = null,
    string? ConsultingRoom = null,
    int MaxConcurrentAppointments = 1);
