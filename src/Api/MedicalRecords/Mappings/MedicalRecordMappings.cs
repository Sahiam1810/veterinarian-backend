using Api.MedicalRecords.Dtos;
using Domain.MedicalRecords.Entities;

namespace Api.MedicalRecords.Mappings;

public static class MedicalRecordMappings
{
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
