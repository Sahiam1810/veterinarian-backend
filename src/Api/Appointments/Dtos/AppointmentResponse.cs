namespace Api.Appointments.Dtos;

public sealed record AppointmentResponse(
    Guid Id,
    Guid ClientPetId,
    Guid VeterinarianId,
    Guid ServiceId,
    string? ServiceName,
    Guid StatusId,
    string? StatusName,
    Guid AvailabilityId,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    string? Notes,
    string? RequesterPhoneNumber,
    DateTime CreatedAt);
