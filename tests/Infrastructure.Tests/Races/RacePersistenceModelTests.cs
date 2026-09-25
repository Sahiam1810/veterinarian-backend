using Domain.Races.Entities;
using Domain.Species.Entities;
using Infrastructure.Persistence;
using Infrastructure.Races.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Infrastructure.Tests.Races;

public sealed class RacePersistenceModelTests
{
    [Fact]
    public void Model_requires_species_with_restrict_delete_and_scoped_unique_name()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new VeterinaryDbContext(options);
        var race = context.Model.FindEntityType(typeof(RaceEntity))!;

        var foreignKey = Assert.Single(
            race.GetForeignKeys(),
            key => key.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(RaceEntity.SpeciesId)]));
        Assert.True(foreignKey.IsRequired);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);

        Assert.Contains(
            race.GetIndexes(),
            index => index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .SequenceEqual([nameof(RaceEntity.SpeciesId), nameof(RaceEntity.Name)]));
    }

    [Fact]
    public async Task Repository_returns_only_races_from_the_requested_species()
    {
        var options = new DbContextOptionsBuilder<VeterinaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new VeterinaryDbContext(options);
        var dog = new SpeciesEntity("Perro");
        var cat = new SpeciesEntity("Gato");
        context.AddRange(
            dog,
            cat,
            new RaceEntity("Labrador Retriever", dog),
            new RaceEntity("Siamés", cat));
        await context.SaveChangesAsync();

        var races = await new RaceRepository(context)
            .GetAllAsync(dog.Id, CancellationToken.None);

        var race = Assert.Single(races);
        Assert.Equal("Labrador Retriever", race.Name.Value);
        Assert.Equal(dog.Id, race.SpeciesId);
    }
}
