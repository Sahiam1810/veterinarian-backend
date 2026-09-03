using Application.Clients.Abstraction;
using Application.Clients.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Users.Abstraction;
using Domain.Clients.Entities;
using NSubstitute;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Clients;

// P1 corregido: un mismo usuario podía terminar con dos perfiles de cliente
// -- Clients.UserId no tenía unique constraint ni chequeo de aplicación,
// dejando a /clients/me (GetByUserIdAsync + FirstOrDefault) no determinístico
// si eso llegaba a pasar.
public sealed class CreateClientCommandHandlerTests
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClientRepository clientsRepository = Substitute.For<IClientRepository>();
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly CreateClientCommandHandler sut;

    public CreateClientCommandHandlerTests()
    {
        unitOfWork.ClientsRepository.Returns(clientsRepository);
        unitOfWork.UsersRepository.Returns(usersRepository);
        sut = new CreateClientCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_throws_conflict_when_the_user_already_has_a_client_profile()
    {
        var user = new UserEntity("Ana Cliente", "ana@huellitas.test", "hash", Guid.NewGuid());
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        clientsRepository.ExistsByIdentificationNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        clientsRepository.ExistsByUserIdAsync(user.Id, Arg.Any<CancellationToken>(), Arg.Any<Guid?>()).Returns(true);

        var command = new CreateClientCommand(user.Id, "1234567890", "Calle Falsa 123");

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));

        await clientsRepository.DidNotReceive().AddAsync(Arg.Any<ClientEntity>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_creates_the_client_when_the_user_has_no_existing_client_profile()
    {
        var user = new UserEntity("Ana Cliente", "ana@huellitas.test", "hash", Guid.NewGuid());
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        clientsRepository.ExistsByIdentificationNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        clientsRepository.ExistsByUserIdAsync(user.Id, Arg.Any<CancellationToken>(), Arg.Any<Guid?>()).Returns(false);

        var command = new CreateClientCommand(user.Id, "1234567890", "Calle Falsa 123");

        var id = await sut.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        await clientsRepository.Received(1).AddAsync(Arg.Any<ClientEntity>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
