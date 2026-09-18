namespace Api.VeterinarianAbsences.Dtos;

public sealed record UpdateVeterinarianAbsenceRequest(
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string? Reason,
    bool IsFullDay);
