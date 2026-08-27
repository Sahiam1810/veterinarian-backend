namespace Api.AppointmentStatusHistories.Dtos;

public sealed record AppointmentStatusHistoryResponse(
    Guid Id,
    Guid AppointmentId,
    Guid StatusId,
    string? StatusName,
    Guid ClientPetId,
    string? Comment,
    DateTime CreatedAt);
