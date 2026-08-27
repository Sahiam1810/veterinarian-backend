namespace Api.Appointments.Dtos;

public sealed record CreateAppointmentRequest(
    Guid ClientPetId,
    Guid VeterinarianId,
    Guid ServiceId,
    Guid StatusId,
    string? Reason,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    string? Notes);
