namespace Api.VeterinarianAbsences.Dtos;

public sealed record CreateVeterinarianAbsenceRequest(
    Guid VeterinarianId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string? Reason,
    bool IsFullDay);
