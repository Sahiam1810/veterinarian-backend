using Api.Species.Dtos;
using Domain.Species.Entities;

namespace Api.Species.Mappings;

public static class SpeciesMappings
{
    public static SpeciesResponseDto ToDto(this SpeciesEntity entity)
    {
        return new SpeciesResponseDto(
            entity.Id,
            entity.Name.Value
        );
    }
}
