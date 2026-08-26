namespace Api.StatusAppointments.Dtos;

public sealed record CreateStatusAppointmentRequest(
    string Name,
    string? Description);
