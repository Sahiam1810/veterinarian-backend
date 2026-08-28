using Api.MedicalRecords.Dtos;
using Application.MedicalRecords.UseCases;
using Domain.MedicalRecords.Entities;

namespace Api.MedicalRecords.Mappings;

public static class MedicalRecordMappings
{
    public static CreateMedicalRecordCommand ToCommand(
        this CreateMedicalRecordRequest request)
    {
        return new CreateMedicalRecordCommand(
            request.ClientPetId,
            request.AppointmentId,
            request.DiagnosticId,
            request.Symptoms,
            request.Treatment,
            request.WeightAtVisit,
            request.Temperature);
    }

    public static MedicalRecordResponse ToResponse(
        this MedicalRecord entity)
    {
        return new MedicalRecordResponse(
            entity.Id,
            entity.ClientPetId,
            entity.AppointmentId,
            entity.DiagnosticId,
            entity.Diagnostic?.Code,
            entity.Symptoms,
            entity.Treatment,
            entity.WeightAtVisit,
            entity.Temperature,
            entity.CreatedAt);
    }

    public static IReadOnlyCollection<MedicalRecordResponse> ToResponse(
        this IReadOnlyCollection<MedicalRecord> entities)
    {
        return entities
            .Select(e => e.ToResponse())
            .ToArray();
    }
}
