using Application.Clients.Abstraction;
using Application.Clients.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Users.Abstraction;
using Domain.Clients.Entities;
using NSubstitute;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;
using Application.Tests.Common;

namespace Application.Tests.Clients;

public sealed class UpdateClientOwnerProfileCommandHandlerTests
{
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClientRepository clientsRepository = Substitute.For<IClientRepository>();
    private readonly IUsersRepository usersRepository = Substitute.For<IUsersRepository>();
    private readonly UpdateClientOwnerProfileCommandHandler sut;

    public UpdateClientOwnerProfileCommandHandlerTests()
    {
        unitOfWork.ClientsRepository.Returns(clientsRepository);
        unitOfWork.UsersRepository.Returns(usersRepository);
        sut = new UpdateClientOwnerProfileCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task Handle_updates_full_name_and_email_without_changing_the_role()
    {
        var roleId = Guid.NewGuid();
        var user = new UserEntity("Carlitos Mesa", "carlitos@gmail.com", null, roleId);
        var client = TestClients.Create(user.Id, "1232222222", "Transversal 2D # 1W - 05");

        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        usersRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(false);

        var command = new UpdateClientOwnerProfileCommand(client.Id, "Carlitos Meza", "carlitos@gmail.com");

        await sut.Handle(command, CancellationToken.None);

        Assert.Equal("Carlitos Meza", user.FullName);
        Assert.Equal(roleId, user.RoleId);
        await usersRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_excludes_its_own_user_id_when_checking_for_a_duplicate_email()
    {
        var user = new UserEntity("Ana Cliente", "ana@huellitas.test", null, Guid.NewGuid());
        var client = TestClients.Create(user.Id, "1234567890", "Calle Falsa 123");

        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        usersRepository.ExistsByEmailAsync("ana@huellitas.test", Arg.Any<CancellationToken>(), user.Id)
            .Returns(false);

        var command = new UpdateClientOwnerProfileCommand(client.Id, "Ana Cliente", "ana@huellitas.test");

        await sut.Handle(command, CancellationToken.None);

        await usersRepository.Received(1).ExistsByEmailAsync("ana@huellitas.test", Arg.Any<CancellationToken>(), user.Id);
    }

    [Fact]
    public async Task Handle_throws_conflict_when_the_new_email_is_already_used_by_another_user()
    {
        var user = new UserEntity("Ana Cliente", "ana@huellitas.test", null, Guid.NewGuid());
        var client = TestClients.Create(user.Id, "1234567890", "Calle Falsa 123");

        clientsRepository.GetByIdAsync(client.Id, Arg.Any<CancellationToken>()).Returns(client);
        usersRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        usersRepository.ExistsByEmailAsync("duplicado@huellitas.test", Arg.Any<CancellationToken>(), user.Id)
            .Returns(true);

        var command = new UpdateClientOwnerProfileCommand(client.Id, "Ana Cliente", "duplicado@huellitas.test");

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));

        await usersRepository.DidNotReceive().UpdateAsync(Arg.Any<UserEntity>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_not_found_when_the_client_does_not_exist()
    {
        clientsRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ClientEntity?)null);

        var command = new UpdateClientOwnerProfileCommand(Guid.NewGuid(), "Alguien", "alguien@huellitas.test");

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }
}
