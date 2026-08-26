namespace Api.Veterinarians.Dtos;

public sealed record CreateVeterinarianRequest(
    Guid UserId,
    Guid SpecialtyId,
    string LicenseNumber);
