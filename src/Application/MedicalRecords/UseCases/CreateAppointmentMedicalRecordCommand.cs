using MediatR;

namespace Application.MedicalRecords.UseCases;

public sealed record CreateAppointmentMedicalRecordVaccinationItem(
    string VaccineName,
    int DoseNumber,
    DateTime ApplicationDate,
    DateTime? NextDoseDate);

public sealed record CreateAppointmentMedicalRecordResult(
    Guid MedicalRecordId,
    Guid AppointmentId,
    IReadOnlyCollection<Guid> VaccinationIds);

public sealed record CreateAppointmentMedicalRecordCommand(
    Guid AppointmentId,
    Guid DiagnosticId,
    string? Symptoms,
    string? Treatment,
    decimal? WeightAtVisit,
    decimal? Temperature,
    IReadOnlyCollection<CreateAppointmentMedicalRecordVaccinationItem>? Vaccinations,
    Guid ActorUserAccountId,
    bool EnforceVeterinarianOwnership) : IRequest<CreateAppointmentMedicalRecordResult>;
