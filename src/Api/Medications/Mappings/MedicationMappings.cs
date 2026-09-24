using Api.Medications.Dtos;
using Application.Medications.UseCases;
using Domain.Medications.Entities;

namespace Api.Medications.Mappings;

public static class MedicationMappings
{
    public static MedicationDto ToResponse(this Medication medication)
    {
        return new MedicationDto(
            medication.Id,
            medication.Name,
            medication.Code,
            medication.IsActive,
            medication.Price,
            medication.CreatedAt,
            medication.UpdatedAt);
    }

    public static IEnumerable<MedicationDto> ToResponse(this IEnumerable<Medication> medications)
    {
        return medications.Select(m => m.ToResponse());
    }

    public static CreateMedicationCommand ToCommand(this CreateMedicationDto dto)
    {
        return new CreateMedicationCommand(dto.Name, dto.Code, dto.IsActive, dto.Price);
    }

    public static UpdateMedicationCommand ToCommand(this UpdateMedicationDto dto, Guid id)
    {
        return new UpdateMedicationCommand(id, dto.Name, dto.Code, dto.IsActive, dto.Price);
    }
}
