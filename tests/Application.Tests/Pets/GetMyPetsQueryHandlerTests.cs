using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Pets.Abstraction;
using Application.Pets.UseCases;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using NSubstitute;
using Xunit;
using Application.Tests.Common;

namespace Application.Tests.Pets;

public sealed class GetMyPetsQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_enriched_profiles_for_owned_pets()
    {
        var client = TestClients.Create("1234567890", null);
        var species = new SpeciesEntity("Canino");
        var race = new RaceEntity("Mestizo", species);
        var pet = new PetEntity("Luna", 4, "F", 12.5m, "Sana", species, race);
        var relation = new ClientPetEntity(client, pet, true);

        var uow = Substitute.For<IUnitOfWork>();
        var clients = Substitute.For<IClientRepository>();
        var clientPets = Substitute.For<IClientPetRepository>();
        var pets = Substitute.For<IPetRepository>();
        uow.ClientsRepository.Returns(clients);
        uow.ClientPetsRepository.Returns(clientPets);
        uow.PetsRepository.Returns(pets);
        clients.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        clientPets.GetByClientIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns([relation]);
        pets.GetByIdsAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { pet.Id })),
            Arg.Any<CancellationToken>()).Returns([pet]);

        var result = await new GetMyPetsQueryHandler(uow)
            .Handle(new GetMyPetsQuery(client.Id), CancellationToken.None);

        var profile = Assert.Single(result);
        Assert.Equal("Luna", profile.Name);
        Assert.Equal("Canino", profile.SpeciesName);
        Assert.Equal("Mestizo", profile.RaceName);
        Assert.Equal(pet.CreatedAt, profile.UpdatedAt);
    }

    [Fact]
    public async Task Handle_never_reads_pets_of_another_client()
    {
        var client = TestClients.Create("1234567890", null);
        var otherClient = TestClients.Create("9876543210", null, phoneNumber: "3009876543", email: "otro@test.com");
        var species = new SpeciesEntity("Canino");
        var race = new RaceEntity("Mestizo", species);
        var ownPet = new PetEntity("Luna", 4, "F", 12.5m, "Sana", species, race);
        var otherPet = new PetEntity("Rex", 6, "M", 20m, "Sano", species, race);

        var uow = Substitute.For<IUnitOfWork>();
        var clients = Substitute.For<IClientRepository>();
        var clientPets = Substitute.For<IClientPetRepository>();
        var pets = Substitute.For<IPetRepository>();
        uow.ClientsRepository.Returns(clients);
        uow.ClientPetsRepository.Returns(clientPets);
        uow.PetsRepository.Returns(pets);
        clients.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        clientPets.GetByClientIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns([new ClientPetEntity(client, ownPet, true)]);
        clientPets.GetByClientIdAsync(otherClient.Id, Arg.Any<CancellationToken>())
            .Returns([new ClientPetEntity(otherClient, otherPet, true)]);
        pets.GetByIdsAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { ownPet.Id })),
            Arg.Any<CancellationToken>()).Returns([ownPet]);

        var result = await new GetMyPetsQueryHandler(uow)
            .Handle(new GetMyPetsQuery(client.Id), CancellationToken.None);

        Assert.Equal("Luna", Assert.Single(result).Name);
        await clientPets.DidNotReceive().GetByClientIdAsync(otherClient.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_not_found_when_the_client_does_not_exist()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var clients = Substitute.For<IClientRepository>();
        uow.ClientsRepository.Returns(clients);
        clients.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ClientEntity?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => new GetMyPetsQueryHandler(uow)
                .Handle(new GetMyPetsQuery(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("Cliente no encontrado.", exception.Message);
    }
}
