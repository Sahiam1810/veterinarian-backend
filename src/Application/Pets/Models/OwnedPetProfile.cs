namespace Application.Pets.Models;

public sealed record OwnedPetProfile(
    Guid Id,
    string Name,
    int Age,
    string Gender,
    decimal Weight,
    string? Observations,
    Guid SpeciesId,
    string SpeciesName,
    Guid RaceId,
    string RaceName,
    DateTime UpdatedAt);
