namespace Api.Appointments.Dtos;

public sealed record CreateAppointmentMedicalRecordVaccinationRequest(
    string VaccineName,
    int DoseNumber,
    DateTime ApplicationDate,
    DateTime? NextDoseDate);

public sealed record CreateAppointmentMedicalRecordRequest(
    Guid DiagnosticId,
    string? Symptoms,
    string? Treatment,
    decimal? WeightAtVisit,
    decimal? Temperature,
    IReadOnlyCollection<CreateAppointmentMedicalRecordVaccinationRequest>? Vaccinations);
