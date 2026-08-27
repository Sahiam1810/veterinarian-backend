namespace Api.Races.Dtos;

public record CreateRaceDto(string Name);

public record UpdateRaceDto(string Name);

public record RaceResponseDto(
    Guid Id,
    string Name
);
