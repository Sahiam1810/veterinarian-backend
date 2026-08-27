using Api.Vaccinations.Dtos;
using Application.Vaccinations.UseCases;
using Domain.Vaccinations.Entities;

namespace Api.Vaccinations.Mappings;

public static class VaccinationMappings
{
    public static CreateVaccinationCommand ToCommand(
        this CreateVaccinationRequest request)
    {
        return new CreateVaccinationCommand(
            request.ClientPetId,
            request.RecordId,
            request.VaccineName,
            request.DoseNumber,
            request.ApplicationDate,
            request.NextDoseDate);
    }

    public static UpdateVaccinationCommand ToCommand(
        this UpdateVaccinationRequest request,
        Guid id)
    {
        return new UpdateVaccinationCommand(
            id,
            request.ClientPetId,
            request.RecordId,
            request.VaccineName,
            request.DoseNumber,
            request.ApplicationDate,
            request.NextDoseDate);
    }

    public static VaccinationResponse ToResponse(
        this Vaccination entity)
    {
        return new VaccinationResponse(
            entity.Id,
            entity.ClientPetId,
            entity.RecordId,
            entity.VaccineName,
            entity.DoseNumber,
            entity.ApplicationDate,
            entity.NextDoseDate,
            entity.CreatedAt);
    }

    public static IReadOnlyCollection<VaccinationResponse> ToResponse(
        this IReadOnlyCollection<Vaccination> entities)
    {
        return entities
            .Select(e => e.ToResponse())
            .ToArray();
    }
}
