using Api.Races.Controllers;
using Api.Races.Dtos;
using Api.Races.Mappings;
using Application.Races.UseCases;
using MediatR;
using NSubstitute;
using Xunit;

namespace Api.Tests.Races;

public sealed class RaceApiContractTests
{
    [Fact]
    public async Task GetAll_forwards_the_optional_species_filter()
    {
        var speciesId = Guid.NewGuid();
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetAllRacesQuery>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var controller = new RacesController(sender);

        await controller.GetAll(speciesId, CancellationToken.None);

        await sender.Received(1).Send(
            Arg.Is<GetAllRacesQuery>(query => query.SpeciesId == speciesId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Create_mapping_includes_the_required_species()
    {
        var speciesId = Guid.NewGuid();

        var command = new CreateRaceDto("Mestizo", speciesId).ToCommand();

        Assert.Equal(speciesId, command.SpeciesId);
    }

    [Fact]
    public void Update_mapping_includes_the_required_species()
    {
        var raceId = Guid.NewGuid();
        var speciesId = Guid.NewGuid();

        var command = new UpdateRaceDto("Mestizo", speciesId).ToCommand(raceId);

        Assert.Equal(raceId, command.Id);
        Assert.Equal(speciesId, command.SpeciesId);
    }
}
