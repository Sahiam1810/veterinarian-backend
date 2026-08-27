namespace Api.AppointmentStatusHistories.Dtos;

public sealed record UpdateAppointmentStatusHistoryRequest(
    Guid AppointmentId,
    Guid StatusId,
    Guid ClientPetId,
    string? Comment);
