using MediatR;

namespace Application.Vaccinations.UseCases;

public sealed record CreateVaccinationCommand(
    Guid ClientPetId,
    Guid RecordId,
    string VaccineName,
    int DoseNumber,
    DateTime ApplicationDate,
    DateTime? NextDoseDate) : IRequest<Guid>;
