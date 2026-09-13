namespace Api.Appointments.Dtos;

public sealed record CancelAppointmentByIdentificationRequest(
    string IdentificationNumber,
    string? Comment = null);

public sealed record RescheduleAppointmentByIdentificationRequest(
    string IdentificationNumber,
    Guid AvailabilityId,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    string RequesterPhoneNumber,
    string? Notes = null);

public sealed record CreateAppointmentByIdentificationRequest(
    string IdentificationNumber,
    Guid PetId,
    Guid VeterinarianId,
    Guid ServiceId,
    DateTime ScheduledStartUtc,
    string? Notes = null,
    string? RequesterPhoneNumber = null);
