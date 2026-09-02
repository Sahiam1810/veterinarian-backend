using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Pets.Abstraction;
using Application.Pets.UseCases;
using Application.Races.Abstraction;
using Application.Species.Abstraction;
using Application.UserAccounts.Abstraction;
using Domain.Clients.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using Domain.UserAccounts.Entities;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.Tests.Pets;

public sealed class UpdateMyPetProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_updates_only_requested_fields_for_owned_pet()
    {
        var fixture = new Fixture();
        var command = new UpdateMyPetProfileCommand(
            fixture.AccountId, fixture.Pet.Id, null, 5, null, 13.2m, null, false,
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
        public Guid AccountId { get; } = Guid.NewGuid();
        public PetEntity Pet { get; }
        public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
        public IPetRepository Pets { get; } = Substitute.For<IPetRepository>();
        public UpdateMyPetProfileCommandHandler Sut { get; }

        public Fixture(bool ownsPet = true)
        {
            var userId = Guid.NewGuid();
            var account = new UserAccountEntity(userId, "cliente", "cliente@test.com", "Active");
            var client = new ClientEntity(userId, "1234567890", null);
            var species = new SpeciesEntity("Canino");
            var race = new RaceEntity("Mestizo");
            Pet = new PetEntity("Luna", 4, "F", 12.5m, "Sana", species, race);

            var accounts = Substitute.For<IUserAccountsRepository>();
            var clients = Substitute.For<IClientRepository>();
            var clientPets = Substitute.For<IClientPetRepository>();
            var speciesRepository = Substitute.For<ISpeciesRepository>();
            var racesRepository = Substitute.For<IRaceRepository>();
            UnitOfWork.UserAccountsRepository.Returns(accounts);
            UnitOfWork.ClientsRepository.Returns(clients);
            UnitOfWork.ClientPetsRepository.Returns(clientPets);
            UnitOfWork.PetsRepository.Returns(Pets);
            UnitOfWork.SpeciesRepository.Returns(speciesRepository);
            UnitOfWork.RacesRepository.Returns(racesRepository);
            accounts.GetByIdAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);
            clients.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(client);
            clientPets.ExistsByClientAndPetAsync(client.Id, Pet.Id, Arg.Any<CancellationToken>())
                .Returns(ownsPet);
            Pets.GetByIdAsync(Pet.Id, Arg.Any<CancellationToken>()).Returns(Pet);
            speciesRepository.GetByIdAsync(species.Id, Arg.Any<CancellationToken>()).Returns(species);
            racesRepository.GetByIdAsync(race.Id, Arg.Any<CancellationToken>()).Returns(race);
            Sut = new UpdateMyPetProfileCommandHandler(UnitOfWork);
        }

        public UpdateMyPetProfileCommand NameChange(string name) => new(
            AccountId, Pet.Id, name, null, null, null, null, false,
            null, null, Pet.UpdatedAt ?? Pet.CreatedAt);
    }
}
