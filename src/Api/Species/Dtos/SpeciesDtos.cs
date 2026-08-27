namespace Api.Species.Dtos;

public record CreateSpeciesDto(string Name);

public record UpdateSpeciesDto(string Name);

public record SpeciesResponseDto(
    Guid Id,
    string Name
);
