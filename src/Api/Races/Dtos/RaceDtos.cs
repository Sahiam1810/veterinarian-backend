namespace Api.Races.Dtos;

public record CreateRaceDto(string Name, Guid SpeciesId);

public record UpdateRaceDto(string Name, Guid SpeciesId);

// SpeciesName facilita listados sin lookup extra en el front
public record RaceResponseDto(
    Guid Id,
    string Name,
    Guid SpeciesId,
    string SpeciesName
);
