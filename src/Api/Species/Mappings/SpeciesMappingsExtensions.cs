using Api.Species.Dtos;
using Application.Species.UseCases;
using Domain.Species.Entities;

namespace Api.Species.Mappings;

public static class SpeciesMappings
{
    public static CreateSpeciesCommand ToCommand(this CreateSpeciesDto dto)
    {
        return new CreateSpeciesCommand(dto.Name);
    }

    public static UpdateSpeciesCommand ToCommand(this UpdateSpeciesDto dto, Guid id)
    {
        return new UpdateSpeciesCommand(id, dto.Name);
    }

    public static SpeciesResponseDto ToDto(this SpeciesEntity entity)
    {
        return new SpeciesResponseDto(
            entity.Id,
            entity.Name.Value
        );
    }

    public static IReadOnlyCollection<SpeciesResponseDto> ToDto(
        this IReadOnlyCollection<SpeciesEntity> entities)
    {
        return entities
            .Select(entity => entity.ToDto())
            .ToArray();
    }
}
