using Api.Races.Dtos;
using veterinarian_backend.Domain.Races.Entities;

namespace Api.Races.Mappings;

public static class RaceMappings
{

    public static RaceResponseDto ToDto(this RaceEntity entity)
    {
        return new RaceResponseDto(
            entity.Id,
            entity.Name
        );
    }

    public static RaceEntity ToEntity(this CreateRaceDto dto)
    {
        return new RaceEntity
        {
            Id = Guid.NewGuid(),
            Name = dto.Name
        };
    }
}