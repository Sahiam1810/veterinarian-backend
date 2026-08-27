namespace Api.Veterinarians.Dtos;

public sealed record UpdateVeterinarianRequest(
    Guid UserId,
    Guid SpecialtyId,
    string LicenseNumber);
