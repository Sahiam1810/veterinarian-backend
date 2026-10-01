namespace Api.Availabilities.Dtos;

public sealed record CreateAvailabilityRequest(
    Guid VeterinarianId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive = true,
    int SlotDurationMinutes = 30,
    string? ShiftName = null,
    string? ConsultingRoom = null,
    int MaxConcurrentAppointments = 1);
