using Api.Species.Dtos;
using veterinarian_backend.Domain.Species.Entities;

namespace Api.Species.Mappings;

public static class SpeciesMappings
{
    public static SpeciesResponseDto ToDto(this SpeciesEntity entity)
    {
        return new SpeciesResponseDto(
            entity.Id,
            entity.Name
        );
    }

    public static SpeciesEntity ToEntity(this CreateSpeciesDto dto)
    {
        return new SpeciesEntity
        {
            Id = Guid.NewGuid(),
            Name = dto.Name
        };
    }
}
