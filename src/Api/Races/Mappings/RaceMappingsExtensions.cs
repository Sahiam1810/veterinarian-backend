using Api.Races.Dtos;
using Application.Races.UseCases;
using Domain.Races.Entities;

namespace Api.Races.Mappings;

public static class RaceMappings
{
    public static CreateRaceCommand ToCommand(this CreateRaceDto dto)
    {
        return new CreateRaceCommand(dto.Name);
    }

    public static UpdateRaceCommand ToCommand(this UpdateRaceDto dto, Guid id)
    {
        return new UpdateRaceCommand(id, dto.Name);
    }

    public static RaceResponseDto ToDto(this RaceEntity entity)
    {
        return new RaceResponseDto(
            entity.Id,
            entity.Name.Value
        );
    }

    public static IReadOnlyCollection<RaceResponseDto> ToDto(
        this IReadOnlyCollection<RaceEntity> entities)
    {
        return entities
            .Select(entity => entity.ToDto())
            .ToArray();
    }
}
