namespace Api.VeterinarianAbsences.Dtos;

public sealed record VeterinarianAbsenceResponse(
    Guid Id,
    Guid VeterinarianId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string? Reason,
    bool IsFullDay,
    DateTime CreatedAt);
