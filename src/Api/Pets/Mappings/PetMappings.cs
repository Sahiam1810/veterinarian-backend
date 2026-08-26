using Api.Pets.Dtos;
using Domain.Pets.Entities;

namespace Api.Pets.Mappings;

public static class PetMappings
{
    public static PetResponseDto ToDto(this PetEntity entity)
    {
        return new PetResponseDto(
            entity.Id,
            entity.Name.Value,
            entity.Age,
            entity.Gender.Value,
            entity.Weight.Value,
            entity.Observations.Value,
            entity.SpeciesId,
            entity.RaceId
        );
    }
}
