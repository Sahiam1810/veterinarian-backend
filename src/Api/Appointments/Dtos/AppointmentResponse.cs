namespace Api.Appointments.Dtos;

public sealed record AppointmentResponse(
    Guid Id,
    Guid ClientPetId,
    string? PetName,
    Guid VeterinarianId,
    string? VeterinarianName,
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
