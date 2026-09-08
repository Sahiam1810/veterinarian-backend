using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;

namespace Tests.Shared;

// Fixtures: RaceEntity exige SpeciesId
public static class TestPets
{
    public static (SpeciesEntity species, RaceEntity race) NewCaninoMestizo()
    {
        var species = new SpeciesEntity("Canino");
        var race = new RaceEntity("Mestizo", species);
        return (species, race);
    }

    public static PetEntity NewCaninoPet(
        string name,
        int age,
        string gender,
        decimal weight,
        string? observations = null)
    {
        var (species, race) = NewCaninoMestizo();
        return new PetEntity(name, age, gender, weight, observations, species, race);
    }
}
