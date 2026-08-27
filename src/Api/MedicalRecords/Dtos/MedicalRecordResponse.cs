namespace Api.MedicalRecords.Dtos;

public sealed record MedicalRecordResponse(
    Guid Id,
    Guid ClientPetId,
    Guid AppointmentId,
    Guid DiagnosticId,
    string? DiagnosticCode,
    string? Symptoms,
    string? Treatment,
    decimal? WeightAtVisit,
    decimal? Temperature,
    DateTime CreatedAt);
