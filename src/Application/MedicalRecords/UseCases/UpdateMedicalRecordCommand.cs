using MediatR;

namespace Application.MedicalRecords.UseCases;

public sealed record UpdateMedicalRecordCommand(
    Guid Id,
    Guid ClientPetId,
    Guid AppointmentId,
    Guid DiagnosticId,
    string? Symptoms,
    string? Treatment,
    decimal? WeightAtVisit,
    decimal? Temperature) : IRequest<bool>;
