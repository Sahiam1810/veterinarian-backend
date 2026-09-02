namespace Api.Appointments.Dtos;

public sealed record UpdateAppointmentStatusRequest(
    Guid StatusId,
    string? Comment);
