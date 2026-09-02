namespace Api.Appointments.Dtos;

public sealed record CreateAppointmentRequest(
    Guid ClientPetId,
    Guid VeterinarianId,
    Guid ServiceId,
    Guid StatusId,
    Guid AvailabilityId,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    string? Notes,
    string RequesterPhoneNumber);
