using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Pets.Abstraction;
using Application.Pets.UseCases;
using Application.UserAccounts.Abstraction;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.Tests.Pets;

public sealed class GetMyPetsQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_enriched_profiles_for_owned_pets()
    {
        var userAccountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var account = new UserAccountEntity(userId, "cliente", "cliente@test.com", "Active");
        var client = new ClientEntity(userId, "1234567890", null);
        var species = new SpeciesEntity("Canino");
        var race = new RaceEntity("Mestizo");
        var pet = new PetEntity("Luna", 4, "F", 12.5m, "Sana", species, race);
        var relation = new ClientPetEntity(client, pet, true);

        var uow = Substitute.For<IUnitOfWork>();
        var accounts = Substitute.For<IUserAccountsRepository>();
        var clients = Substitute.For<IClientRepository>();
        var clientPets = Substitute.For<IClientPetRepository>();
        var pets = Substitute.For<IPetRepository>();
        uow.UserAccountsRepository.Returns(accounts);
        uow.ClientsRepository.Returns(clients);
        uow.ClientPetsRepository.Returns(clientPets);
        uow.PetsRepository.Returns(pets);
        accounts.GetByIdAsync(userAccountId, Arg.Any<CancellationToken>()).Returns(account);
        clients.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(client);
        clientPets.GetByClientIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns([relation]);
        pets.GetByIdsAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { pet.Id })),
            Arg.Any<CancellationToken>()).Returns([pet]);

        var result = await new GetMyPetsQueryHandler(uow)
            .Handle(new GetMyPetsQuery(userAccountId), CancellationToken.None);

        var profile = Assert.Single(result);
        Assert.Equal("Luna", profile.Name);
        Assert.Equal("Canino", profile.SpeciesName);
        Assert.Equal("Mestizo", profile.RaceName);
        Assert.Equal(pet.CreatedAt, profile.UpdatedAt);
    }
}
