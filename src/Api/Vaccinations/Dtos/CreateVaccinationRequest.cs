namespace Api.Vaccinations.Dtos;

public sealed record CreateVaccinationRequest(
    Guid ClientPetId,
    Guid RecordId,
    string VaccineName,
    int DoseNumber,
    DateTime ApplicationDate,
    DateTime? NextDoseDate);
