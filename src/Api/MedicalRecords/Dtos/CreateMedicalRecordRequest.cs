namespace Api.MedicalRecords.Dtos;

public sealed record CreateMedicalRecordRequest(
    Guid ClientPetId,
    Guid AppointmentId,
    Guid DiagnosticId,
    string? Symptoms,
    string? Treatment,
    decimal? WeightAtVisit,
    decimal? Temperature);
