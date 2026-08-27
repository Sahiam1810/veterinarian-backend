namespace Api.Vaccinations.Dtos;

public sealed record UpdateVaccinationRequest(
    Guid ClientPetId,
    Guid RecordId,
    string VaccineName,
    int DoseNumber,
    DateTime ApplicationDate,
    DateTime? NextDoseDate);
