namespace Api.AppointmentStatusHistories.Dtos;

public sealed record CreateAppointmentStatusHistoryRequest(
    Guid AppointmentId,
    Guid StatusId,
    Guid ClientPetId,
    string? Comment);
