namespace Api.Races.Dtos;

public record CreateRaceDto(string Name, Guid SpeciesId);

public record UpdateRaceDto(string Name, Guid SpeciesId);

public record RaceResponseDto(
    Guid Id,
    string Name,
    Guid SpeciesId
);
