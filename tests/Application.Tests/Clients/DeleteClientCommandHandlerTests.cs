using Application.Clients.Abstraction;
using Application.Clients.UseCases;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Tests.Common;
using Domain.Clients.Entities;
using Domain.ClientsPets.Entities;
using Domain.Pets.Entities;
using Domain.Races.Entities;
using Domain.Species.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Clients;

// Frente 1: CLIENTS es independiente de USERS; borrar un cliente nunca toca USERS.
public sealed class DeleteClientCommandHandlerTests
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClientRepository clientsRepository = Substitute.For<IClientRepository>();
    private readonly IClientPetRepository clientPetsRepository = Substitute.For<IClientPetRepository>();
    private readonly DeleteClientCommandHandler sut;

    public DeleteClientCommandHandlerTests()
    {
        unitOfWork.ClientsRepository.Returns(clientsRepository);
        unitOfWork.ClientPetsRepository.Returns(clientPetsRepository);
        clientPetsRepository.GetByClientIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ClientPetEntity>());
        sut = new DeleteClientCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Acceptance_Deleting_a_client_does_not_touch_users()
    {
        var client = TestClients.Create();
        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);

        await sut.Handle(new DeleteClientCommand(client.Id), CancellationToken.None);

        await clientsRepository.Received(1).DeleteAsync(client, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        UnitOfWorkAssertions.AssertOnlyRepositoriesAccessed(
            unitOfWork,
            nameof(IUnitOfWork.ClientsRepository),
            nameof(IUnitOfWork.ClientPetsRepository));
    }

    [Fact]
    public async Task Deleting_a_client_with_pets_is_rejected_and_nothing_is_deleted()
    {
        var client = TestClients.Create();
        var species = new SpeciesEntity("Canino");
        var pet = new PetEntity("Luna", 4, "F", 12m, null, species, new RaceEntity("Mestizo", species));
        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        clientPetsRepository.GetByClientIdAsync(client.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { new ClientPetEntity(client, pet, true) });

        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.Handle(new DeleteClientCommand(client.Id), CancellationToken.None));

        await clientsRepository.DidNotReceive().DeleteAsync(Arg.Any<ClientEntity>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deleting_an_unknown_client_is_not_found()
    {
        clientsRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ClientEntity?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.Handle(new DeleteClientCommand(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal("Cliente no encontrado.", exception.Message);
        await clientsRepository.DidNotReceive().DeleteAsync(Arg.Any<ClientEntity>(), Arg.Any<CancellationToken>());
    }
}
