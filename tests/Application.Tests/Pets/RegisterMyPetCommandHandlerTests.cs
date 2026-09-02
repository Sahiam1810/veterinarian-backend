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
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using Domain.UserAccounts.Entities;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.Tests.Pets;

public sealed class RegisterMyPetCommandHandlerTests
{
    [Fact]
    public async Task Handle_creates_pet_and_primary_owner_in_one_unit_of_work()
    {
        var fixture = new Fixture();

        var result = await fixture.Sut.Handle(fixture.Command, CancellationToken.None);

        Assert.Equal("Luna", result.Name);
        Assert.Equal("Canino", result.SpeciesName);
        Assert.Equal("Mestizo", result.RaceName);
        await fixture.Pets.Received(1).AddAsync(
            Arg.Is<PetEntity>(pet => pet.Name.Value == "Luna"),
            Arg.Any<CancellationToken>());
        await fixture.ClientPets.Received(1).AddAsync(
            Arg.Is<ClientPetEntity>(relation =>
                relation.ClientId == fixture.Client.Id
                && relation.PetId == result.Id
                && relation.IsPrimaryOwner.Value),
            Arg.Any<CancellationToken>());
        await fixture.UnitOfWork.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_account_without_client_profile_before_creating_pet()
    {
        var fixture = new Fixture(hasClient: false);

        await Assert.ThrowsAsync<NotFoundException>(
            () => fixture.Sut.Handle(fixture.Command, CancellationToken.None));

        await fixture.Pets.DidNotReceive().AddAsync(
            Arg.Any<PetEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_unknown_species_before_creating_pet()
    {
        var fixture = new Fixture(hasSpecies: false);

        await Assert.ThrowsAsync<NotFoundException>(
            () => fixture.Sut.Handle(fixture.Command, CancellationToken.None));

        await fixture.Pets.DidNotReceive().AddAsync(
            Arg.Any<PetEntity>(), Arg.Any<CancellationToken>());
    }

    private sealed class Fixture
    {
        public Guid AccountId { get; } = Guid.NewGuid();
        public ClientEntity Client { get; }
        public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
        public IPetRepository Pets { get; } = Substitute.For<IPetRepository>();
        public IClientPetRepository ClientPets { get; } = Substitute.For<IClientPetRepository>();
        public RegisterMyPetCommand Command { get; }
        public RegisterMyPetCommandHandler Sut { get; }

        public Fixture(bool hasClient = true, bool hasSpecies = true)
        {
            var userId = Guid.NewGuid();
            var account = new UserAccountEntity(userId, "cliente", "cliente@test.com", "Active");
            Client = new ClientEntity(userId, "1234567890", null);
            var species = new SpeciesEntity("Canino");
            var race = new RaceEntity("Mestizo");

            var accounts = Substitute.For<IUserAccountsRepository>();
            var clients = Substitute.For<IClientRepository>();
            var speciesRepository = Substitute.For<ISpeciesRepository>();
            var racesRepository = Substitute.For<IRaceRepository>();
            UnitOfWork.UserAccountsRepository.Returns(accounts);
            UnitOfWork.ClientsRepository.Returns(clients);
            UnitOfWork.SpeciesRepository.Returns(speciesRepository);
            UnitOfWork.RacesRepository.Returns(racesRepository);
            UnitOfWork.PetsRepository.Returns(Pets);
            UnitOfWork.ClientPetsRepository.Returns(ClientPets);
            UnitOfWork.ExecuteInTransactionAsync(
                    Arg.Any<Func<CancellationToken, Task>>(),
                    Arg.Any<CancellationToken>())
                .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(
                    call.ArgAt<CancellationToken>(1)));

            accounts.GetByIdAsync(AccountId, Arg.Any<CancellationToken>()).Returns(account);
            clients.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
                .Returns(hasClient ? Client : null);
            speciesRepository.GetByIdAsync(species.Id, Arg.Any<CancellationToken>())
                .Returns(hasSpecies ? species : null);
            racesRepository.GetByIdAsync(race.Id, Arg.Any<CancellationToken>()).Returns(race);

            Command = new RegisterMyPetCommand(
                AccountId, "Luna", 4, "F", 12.5m, "Sana", species.Id, race.Id);
            Sut = new RegisterMyPetCommandHandler(UnitOfWork);
        }
    }
}
