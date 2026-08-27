using MediatR;

namespace Application.MedicalRecords.UseCases;

public sealed record CreateMedicalRecordCommand(
    Guid ClientPetId,
    Guid AppointmentId,
    Guid DiagnosticId,
    string? Symptoms,
    string? Treatment,
    decimal? WeightAtVisit,
    decimal? Temperature) : IRequest<Guid>;
