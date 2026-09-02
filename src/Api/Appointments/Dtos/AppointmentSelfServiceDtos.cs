namespace Api.Appointments.Dtos;

public sealed record CancelMyAppointmentRequest(string? Comment);

public sealed record RequestAppointmentActionCodeRequest(
    string PhoneNumber,
    string Action,
    AppointmentRescheduleRequest? Reschedule = null);

public sealed record AppointmentRescheduleRequest(
    Guid AvailabilityId,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    string? Notes);

public sealed record RequestAppointmentActionCodeResponse(Guid SessionId);

public sealed record ConfirmAppointmentActionCodeRequest(
    string PhoneNumber,
    string Code,
    string Action,
    string? Comment = null);
