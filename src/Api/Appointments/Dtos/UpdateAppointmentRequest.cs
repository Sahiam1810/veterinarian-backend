namespace Api.Appointments.Dtos;

public sealed record UpdateAppointmentRequest(
    Guid ClientPetId,
    Guid VeterinarianId,
    Guid ServiceId,
    Guid StatusId,
    string? Reason,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    string? Notes);
