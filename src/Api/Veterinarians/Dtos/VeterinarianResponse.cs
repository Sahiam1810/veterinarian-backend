namespace Api.Veterinarians.Dtos;

public sealed record VeterinarianResponse(
    Guid Id,
    Guid UserId,
    string? UserFullName,
    Guid SpecialtyId,
    string? SpecialtyName,
    string LicenseNumber,
    DateTime CreatedAt);
