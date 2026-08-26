using Api.Races.Dtos;
using Domain.Races.Entities;

namespace Api.Races.Mappings;

public static class RaceMappings
{
    public static RaceResponseDto ToDto(this RaceEntity entity)
    {
        return new RaceResponseDto(
            entity.Id,
            entity.Name.Value
        );
    }
}