namespace Api.StatusAppointments.Dtos;

public sealed record StatusAppointmentResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt);
