using Api.Pets.Dtos;
using Domain.Pets.Entities;
using Application.Pets.Models;

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

    public static OwnedPetProfileResponseDto ToDto(this OwnedPetProfile profile) => new(
        profile.Id,
        profile.Name,
        profile.Age,
        profile.Gender,
        profile.Weight,
        profile.Observations,
        profile.SpeciesId,
        profile.SpeciesName,
        profile.RaceId,
        profile.RaceName,
        profile.UpdatedAt);
}
