using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Pets.Abstraction;
using Application.Pets.UseCases;
using Application.Races.Abstraction;
using Application.Species.Abstraction;
using Domain.Clients.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using NSubstitute;
using Xunit;
using Application.Tests.Common;

namespace Application.Tests.Pets;

public sealed class UpdateMyPetProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_updates_only_requested_fields_for_owned_pet()
    {
        var fixture = new Fixture();
        var command = new UpdateMyPetProfileCommand(
            fixture.ClientId, fixture.Pet.Id, null, 5, null, 13.2m, null, false,
            null, null, fixture.Pet.CreatedAt);

        var result = await fixture.Sut.Handle(command, CancellationToken.None);

        Assert.Equal("Luna", result.Name);
        Assert.Equal(5, result.Age);
        Assert.Equal(13.2m, result.Weight);
        Assert.Equal("Sana", result.Observations);
        await fixture.Pets.Received(1).UpdateAsync(fixture.Pet, Arg.Any<CancellationToken>());
        await fixture.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_hides_a_pet_not_owned_by_authenticated_client()
    {
        var fixture = new Fixture(ownsPet: false);
        var command = fixture.NameChange("Nueva");

        await Assert.ThrowsAsync<NotFoundException>(
            () => fixture.Sut.Handle(command, CancellationToken.None));
        await fixture.Pets.DidNotReceive().UpdateAsync(Arg.Any<PetEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_unknown_client()
    {
        var fixture = new Fixture(hasClient: false);
        var command = fixture.NameChange("Nueva");

        await Assert.ThrowsAsync<NotFoundException>(
            () => fixture.Sut.Handle(command, CancellationToken.None));
        await fixture.Pets.DidNotReceive().UpdateAsync(Arg.Any<PetEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_a_stale_profile_version()
    {
        var fixture = new Fixture();
        var command = fixture.NameChange("Nueva") with
        {
            ExpectedUpdatedAt = fixture.Pet.CreatedAt.AddMinutes(-1)
        };

        await Assert.ThrowsAsync<ConflictException>(
            () => fixture.Sut.Handle(command, CancellationToken.None));
        await fixture.Pets.DidNotReceive().UpdateAsync(Arg.Any<PetEntity>(), Arg.Any<CancellationToken>());
    }

    private sealed class Fixture
    {
        public Guid ClientId { get; }
        public PetEntity Pet { get; }
        public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
        public IPetRepository Pets { get; } = Substitute.For<IPetRepository>();
        public UpdateMyPetProfileCommandHandler Sut { get; }

        public Fixture(bool ownsPet = true, bool hasClient = true)
        {
            var client = TestClients.Create("1234567890", null);
            ClientId = client.Id;
            var species = new SpeciesEntity("Canino");
            var race = new RaceEntity("Mestizo", species);
            Pet = new PetEntity("Luna", 4, "F", 12.5m, "Sana", species, race);

            var clients = Substitute.For<IClientRepository>();
            var clientPets = Substitute.For<IClientPetRepository>();
            var speciesRepository = Substitute.For<ISpeciesRepository>();
            var racesRepository = Substitute.For<IRaceRepository>();
            UnitOfWork.ClientsRepository.Returns(clients);
            UnitOfWork.ClientPetsRepository.Returns(clientPets);
            UnitOfWork.PetsRepository.Returns(Pets);
            UnitOfWork.SpeciesRepository.Returns(speciesRepository);
            UnitOfWork.RacesRepository.Returns(racesRepository);
            clients.GetByIdAsync(client.Id, Arg.Any<CancellationToken>())
                .Returns(hasClient ? client : null);
            clientPets.ExistsByClientAndPetAsync(client.Id, Pet.Id, Arg.Any<CancellationToken>())
                .Returns(ownsPet);
            Pets.GetByIdAsync(Pet.Id, Arg.Any<CancellationToken>()).Returns(Pet);
            speciesRepository.GetByIdAsync(species.Id, Arg.Any<CancellationToken>()).Returns(species);
            racesRepository.GetByIdAsync(race.Id, Arg.Any<CancellationToken>()).Returns(race);
            Sut = new UpdateMyPetProfileCommandHandler(UnitOfWork);
        }

        public UpdateMyPetProfileCommand NameChange(string name) => new(
            ClientId, Pet.Id, name, null, null, null, null, false,
            null, null, Pet.UpdatedAt ?? Pet.CreatedAt);
    }
}
