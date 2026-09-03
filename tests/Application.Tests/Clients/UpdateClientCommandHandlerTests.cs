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

public sealed class UpdateClientCommandHandlerTests
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClientRepository clientsRepository = Substitute.For<IClientRepository>();
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly UpdateClientCommandHandler sut;

    public UpdateClientCommandHandlerTests()
    {
        unitOfWork.ClientsRepository.Returns(clientsRepository);
        unitOfWork.UsersRepository.Returns(usersRepository);
        sut = new UpdateClientCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_throws_conflict_when_the_new_user_already_has_another_client_profile()
    {
        var client = new ClientEntity(Guid.NewGuid(), "1234567890", "Calle Falsa 123");
        var newUser = new UserEntity("Otro Usuario", "otro@huellitas.test", "hash", Guid.NewGuid());

        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        usersRepository.GetByIdAsync(newUser.Id, Arg.Any<CancellationToken>()).Returns(newUser);
        clientsRepository.ExistsByIdentificationNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        clientsRepository.ExistsByUserIdAsync(newUser.Id, Arg.Any<CancellationToken>(), client.Id).Returns(true);

        var command = new UpdateClientCommand(client.Id, newUser.Id, "1234567890", "Calle Falsa 123");

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));

        await clientsRepository.DidNotReceive().UpdateAsync(Arg.Any<ClientEntity>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_excludes_its_own_id_when_checking_for_a_duplicate_user()
    {
        var client = new ClientEntity(Guid.NewGuid(), "1234567890", "Calle Falsa 123");
        var user = new UserEntity("Ana Cliente", "ana@huellitas.test", "hash", Guid.NewGuid());

        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        clientsRepository.ExistsByIdentificationNumberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);
        clientsRepository.ExistsByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);

        var command = new UpdateClientCommand(client.Id, user.Id, "1234567890", "Calle Falsa 123");

        await sut.Handle(command, CancellationToken.None);

        await clientsRepository.Received(1).ExistsByUserIdAsync(user.Id, Arg.Any<CancellationToken>(), client.Id);
        await clientsRepository.Received(1).UpdateAsync(client, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
