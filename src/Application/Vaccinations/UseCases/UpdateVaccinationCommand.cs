using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed record UpdateVaccinationCommand(
    Guid Id,
    Guid ClientPetId,
    Guid RecordId,
    string VaccineName,
    int DoseNumber,
    DateTime ApplicationDate,
    DateTime? NextDoseDate) : IRequest;
