using Application.Clients.Abstraction;
using Application.Clients.Errors;
using Application.Clients.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Clients.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Clients;

// El cliente se crea con sus propios datos: no busca ni exige ningún usuario.
public sealed class CreateClientCommandHandlerTests
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClientRepository clientsRepository = Substitute.For<IClientRepository>();
    private readonly CreateClientCommandHandler sut;

    public CreateClientCommandHandlerTests()
    {
        unitOfWork.ClientsRepository.Returns(clientsRepository);
        clientsRepository.ExistsByIdentificationNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        clientsRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        clientsRepository.ExistsByPhoneAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        sut = new CreateClientCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_creates_the_client_with_its_own_data_and_without_a_user()
    {
        ClientEntity? created = null;
        clientsRepository.AddAsync(Arg.Any<ClientEntity>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                created = call.ArgAt<ClientEntity>(0);
                return Task.CompletedTask;
            });

        var command = new CreateClientCommand(
            "  Ana Cliente ", "Ana@Huellitas.Test", "1234567890", "3001234567", "Calle Falsa 123");

        var id = await sut.Handle(command, CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal(created!.Id, id);
        Assert.Equal("Ana Cliente", created.FullName.Value);
        Assert.Equal("ana@huellitas.test", created.Email.Value);
        Assert.Null(created.UserId);
        Assert.True(created.IsActive);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // El handler pasa por ClientPhoneNumber.Create (solo dígitos).
    [Fact]
    public async Task Handle_persists_the_client_with_a_normalized_phone_number()
    {
        ClientEntity? created = null;
        clientsRepository.AddAsync(Arg.Any<ClientEntity>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                created = call.ArgAt<ClientEntity>(0);
                return Task.CompletedTask;
            });

        var command = new CreateClientCommand(
            "Ana Cliente", "ana2@huellitas.test", "1234567891", "+57 (300) 123-4567", "Calle Falsa 123");

        await sut.Handle(command, CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("573001234567", created!.PhoneNumber.Value);
        await clientsRepository.Received(1).ExistsByPhoneAsync(
            "573001234567", Arg.Any<CancellationToken>(), Arg.Any<Guid?>());
    }

    [Fact]
    public async Task Handle_throws_identification_conflict_with_its_code()
    {
        clientsRepository.ExistsByIdentificationNumberAsync("1234567890", Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(
            new CreateClientCommand("Ana", "ana@huellitas.test", "1234567890", "3001234567"),
            CancellationToken.None));

        Assert.Equal(ClientErrorCodes.IdentificationAlreadyInUse, ex.Code);
        await AssertNothingPersistedAsync();
    }

    [Fact]
    public async Task Handle_throws_email_conflict_with_its_code()
    {
        clientsRepository.ExistsByEmailAsync("ana@huellitas.test", Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(
            new CreateClientCommand("Ana", "ana@huellitas.test", "1234567890", "3001234567"),
            CancellationToken.None));

        Assert.Equal(ClientErrorCodes.EmailAlreadyInUse, ex.Code);
        await AssertNothingPersistedAsync();
    }

    [Fact]
    public async Task Handle_throws_phone_conflict_with_its_code()
    {
        clientsRepository.ExistsByPhoneAsync("573001234567", Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(
            new CreateClientCommand("Ana", "ana@huellitas.test", "1234567892", "+57 (300) 123-4567"),
            CancellationToken.None));

        Assert.Equal(ClientErrorCodes.PhoneAlreadyInUse, ex.Code);
        await AssertNothingPersistedAsync();
    }

    [Fact]
    public async Task Handle_throws_typed_conflict_when_equivalent_phone_formats_collide()
    {
        clientsRepository.ExistsByPhoneAsync("573001234567", Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(
            new CreateClientCommand("Ana", "ana@huellitas.test", "1234567893", "+57-300-123-4567"),
            CancellationToken.None));

        Assert.Equal(ClientErrorCodes.PhoneAlreadyInUse, ex.Code);
    }

    private async Task AssertNothingPersistedAsync()
    {
        await clientsRepository.DidNotReceive().AddAsync(Arg.Any<ClientEntity>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
