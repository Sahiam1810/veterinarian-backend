namespace Api.Appointments.Dtos;

public sealed record RescheduleMyAppointmentRequest(
    Guid AvailabilityId,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    string RequesterPhoneNumber,
    string? Notes = null);

public sealed record CancelMyAppointmentRequest(string? Comment = null);
