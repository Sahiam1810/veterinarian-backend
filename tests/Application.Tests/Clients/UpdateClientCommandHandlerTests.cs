using Application.Clients.Abstraction;
using Application.Clients.Errors;
using Application.Clients.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Tests.Common;
using Domain.Clients.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Clients;

public sealed class UpdateClientCommandHandlerTests
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClientRepository clientsRepository = Substitute.For<IClientRepository>();
    private readonly UpdateClientCommandHandler sut;

    public UpdateClientCommandHandlerTests()
    {
        unitOfWork.ClientsRepository.Returns(clientsRepository);
        clientsRepository.ExistsByIdentificationNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        clientsRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        clientsRepository.ExistsByPhoneAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        sut = new UpdateClientCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_updates_name_email_and_contact_data_in_a_single_call()
    {
        var client = TestClients.Create("1234567890", "Calle Falsa 123");
        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);

        await sut.Handle(
            new UpdateClientCommand(
                client.Id, "  Ana Nueva ", "Ana.Nueva@Huellitas.Test", "1234567890", "3001234567", "Otra calle", true),
            CancellationToken.None);

        Assert.Equal("Ana Nueva", client.FullName.Value);
        Assert.Equal("ana.nueva@huellitas.test", client.Email.Value);
        Assert.Equal("Otra calle", client.Address?.Value);
        await clientsRepository.Received(1).UpdateAsync(client, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_excludes_its_own_id_when_checking_email_identification_and_phone()
    {
        var client = TestClients.Create("1234567890", "Calle Falsa 123", phoneNumber: "3001234567", email: "ana@huellitas.test");
        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);

        // Editar sin cambiar el correo, la cédula ni el teléfono no es un conflicto.
        await sut.Handle(
            new UpdateClientCommand(
                client.Id, "Ana", "ana@huellitas.test", "1234567890", "3001234567", null, true),
            CancellationToken.None);

        await clientsRepository.Received(1).ExistsByEmailAsync(
            "ana@huellitas.test", Arg.Any<CancellationToken>(), client.Id);
        await clientsRepository.Received(1).ExistsByIdentificationNumberAsync(
            "1234567890", Arg.Any<CancellationToken>(), client.Id);
        await clientsRepository.Received(1).ExistsByPhoneAsync(
            "3001234567", Arg.Any<CancellationToken>(), client.Id);
        await clientsRepository.Received(1).UpdateAsync(client, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_applies_IsActive_with_Deactivate_and_Activate()
    {
        var client = TestClients.Create("1234567890");
        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);

        await sut.Handle(
            new UpdateClientCommand(client.Id, "Ana", "ana@huellitas.test", "1234567890", "3001234567", null, false),
            CancellationToken.None);
        Assert.False(client.IsActive);

        await sut.Handle(
            new UpdateClientCommand(client.Id, "Ana", "ana@huellitas.test", "1234567890", "3001234567", null, true),
            CancellationToken.None);
        Assert.True(client.IsActive);
    }

    [Fact]
    public async Task Handle_persists_the_updated_client_with_a_normalized_phone_number()
    {
        var client = TestClients.Create("1234567890", "Calle Falsa 123", phoneNumber: "3001234567");
        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);

        await sut.Handle(
            new UpdateClientCommand(
                client.Id, "Ana", "ana@huellitas.test", "1234567890", "+57 (301) 555-0000", "Calle Falsa 123", true),
            CancellationToken.None);

        Assert.Equal("573015550000", client.PhoneNumber.Value);
    }

    [Fact]
    public async Task Handle_throws_not_found_when_the_client_does_not_exist()
    {
        clientsRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ClientEntity?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(
            new UpdateClientCommand(
                Guid.NewGuid(), "Ana", "ana@huellitas.test", "1234567890", "3001234567", null, true),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_throws_identification_conflict_with_its_code()
    {
        var client = TestClients.Create("1234567890");
        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        clientsRepository.ExistsByIdentificationNumberAsync("9999999999", Arg.Any<CancellationToken>(), client.Id)
            .Returns(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(
            new UpdateClientCommand(
                client.Id, "Ana", "ana@huellitas.test", "9999999999", "3001234567", null, true),
            CancellationToken.None));

        Assert.Equal(ClientErrorCodes.IdentificationAlreadyInUse, ex.Code);
        await AssertNothingPersistedAsync();
    }

    [Fact]
    public async Task Handle_throws_email_conflict_with_its_code()
    {
        var client = TestClients.Create("1234567890");
        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        clientsRepository.ExistsByEmailAsync("otra@huellitas.test", Arg.Any<CancellationToken>(), client.Id)
            .Returns(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(
            new UpdateClientCommand(
                client.Id, "Ana", "otra@huellitas.test", "1234567890", "3001234567", null, true),
            CancellationToken.None));

        Assert.Equal(ClientErrorCodes.EmailAlreadyInUse, ex.Code);
        await AssertNothingPersistedAsync();
    }

    [Fact]
    public async Task Handle_throws_typed_conflict_when_another_client_has_the_phone()
    {
        var client = TestClients.Create("1234567890", phoneNumber: "3001112233");
        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        clientsRepository.ExistsByPhoneAsync("3009998877", Arg.Any<CancellationToken>(), client.Id)
            .Returns(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(
            new UpdateClientCommand(
                client.Id, "Ana", "ana@huellitas.test", "1234567890", "3009998877", null, true),
            CancellationToken.None));

        Assert.Equal(ClientErrorCodes.PhoneAlreadyInUse, ex.Code);
        await AssertNothingPersistedAsync();
    }

    private async Task AssertNothingPersistedAsync()
    {
        await clientsRepository.DidNotReceive().UpdateAsync(Arg.Any<ClientEntity>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
