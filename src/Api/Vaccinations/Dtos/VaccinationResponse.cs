namespace Api.Vaccinations.Dtos;

public sealed record VaccinationResponse(
    Guid Id,
    Guid ClientPetId,
    Guid RecordId,
    string VaccineName,
    int DoseNumber,
    DateTime ApplicationDate,
    DateTime? NextDoseDate,
    DateTime CreatedAt);
