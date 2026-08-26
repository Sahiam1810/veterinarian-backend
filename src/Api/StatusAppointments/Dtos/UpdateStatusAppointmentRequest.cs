namespace Api.StatusAppointments.Dtos;

public sealed record UpdateStatusAppointmentRequest(
    string Name,
    string? Description);
