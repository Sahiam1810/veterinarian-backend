using Domain.Races.Entities;
using Domain.Pets.Entities;
using Domain.Species.Entities;
using Xunit;

namespace Application.Tests.Races;

public sealed class RaceEntityTests
{
    [Fact]
    public void Constructor_assigns_the_required_species()
    {
        var species = new SpeciesEntity("Perro");

        var race = new RaceEntity("Labrador Retriever", species);

        Assert.Equal(species.Id, race.SpeciesId);
        Assert.Same(species, race.Species);
    }

    [Fact]
    public void Update_changes_name_and_species()
    {
        var dog = new SpeciesEntity("Perro");
        var cat = new SpeciesEntity("Gato");
        var race = new RaceEntity("Mestizo", dog);

        race.Update("Siamés", cat);

        Assert.Equal("Siamés", race.Name.Value);
        Assert.Equal(cat.Id, race.SpeciesId);
        Assert.Same(cat, race.Species);
        Assert.NotNull(race.UpdatedAt);
    }

    [Fact]
    public void Pet_rejects_a_race_from_another_species()
    {
        var dog = new SpeciesEntity("Perro");
        var cat = new SpeciesEntity("Gato");
        var catRace = new RaceEntity("Siamés", cat);

        var action = () => new PetEntity("Luna", 2, "F", 4m, null, dog, catRace);

        Assert.Throws<ArgumentException>(action);
    }
}
