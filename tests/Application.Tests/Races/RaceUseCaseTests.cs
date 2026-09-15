using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Races.Abstraction;
using Application.Races.UseCases;
using Application.Species.Abstraction;
using Domain.Races.Entities;
using Domain.Species.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Races;

public sealed class RaceUseCaseTests
{
    [Fact]
    public async Task GetAll_filters_races_by_species_when_requested()
    {
        var speciesId = Guid.NewGuid();
        var uow = CreateUnitOfWork(out var races, out _);
        races.GetAllAsync(speciesId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<RaceEntity>());

        var result = await new GetAllRacesQueryHandler(uow)
            .Handle(new GetAllRacesQuery(speciesId), CancellationToken.None);

        Assert.Empty(result);
        await races.Received(1).GetAllAsync(speciesId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_rejects_an_unknown_species()
    {
        var uow = CreateUnitOfWork(out var races, out var species);
        var speciesId = Guid.NewGuid();
        species.GetByIdAsync(speciesId, Arg.Any<CancellationToken>()).Returns((SpeciesEntity?)null);

        var action = () => new CreateRaceCommandHandler(uow)
            .Handle(new CreateRaceCommand("Mestizo", speciesId), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(action);
        await races.DidNotReceive().AddAsync(Arg.Any<RaceEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_allows_the_same_race_name_for_a_different_species()
    {
        var dog = new SpeciesEntity("Perro");
        var uow = CreateUnitOfWork(out var races, out var species);
        species.GetByIdAsync(dog.Id, Arg.Any<CancellationToken>()).Returns(dog);
        races.ExistsByNameAsync("Mestizo", dog.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        var id = await new CreateRaceCommandHandler(uow)
            .Handle(new CreateRaceCommand("Mestizo", dog.Id), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        await races.Received(1).AddAsync(
            Arg.Is<RaceEntity>(race => race.SpeciesId == dog.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_checks_uniqueness_within_the_target_species()
    {
        var dog = new SpeciesEntity("Perro");
        var cat = new SpeciesEntity("Gato");
        var race = new RaceEntity("Mestizo", dog);
        var uow = CreateUnitOfWork(out var races, out var species);
        races.GetByIdAsync(race.Id, Arg.Any<CancellationToken>()).Returns(race);
        species.GetByIdAsync(cat.Id, Arg.Any<CancellationToken>()).Returns(cat);
        races.ExistsByNameAsync("Siamés", cat.Id, Arg.Any<CancellationToken>(), race.Id)
            .Returns(false);

        await new UpdateRaceCommandHandler(uow)
            .Handle(new UpdateRaceCommand(race.Id, "Siamés", cat.Id), CancellationToken.None);

        Assert.Equal(cat.Id, race.SpeciesId);
        await races.Received(1).UpdateAsync(race, Arg.Any<CancellationToken>());
    }

    private static IUnitOfWork CreateUnitOfWork(
        out IRaceRepository races,
        out ISpeciesRepository species)
    {
        var uow = Substitute.For<IUnitOfWork>();
        races = Substitute.For<IRaceRepository>();
        species = Substitute.For<ISpeciesRepository>();
        uow.RacesRepository.Returns(races);
        uow.SpeciesRepository.Returns(species);
        return uow;
    }
}
