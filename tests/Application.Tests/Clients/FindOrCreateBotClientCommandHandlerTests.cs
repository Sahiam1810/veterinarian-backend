using Application.Clients.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Telegram.Abstractions;
using Application.Telegram.Models;
using Application.UserAccounts.Abstraction;
using Application.Users.Abstraction;
using Application.Clients.Abstraction;
using Domain.Clients.Entities;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Clients;

public sealed class FindOrCreateBotClientCommandHandlerTests
{
    [Fact]
    public async Task Existing_cedula_reuses_client_without_creating_duplicate()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var clients = Substitute.For<IClientRepository>();
        var users = Substitute.For<IUsersRepository>();
        var accounts = Substitute.For<IUserAccountsRepository>();
        var identity = Substitute.For<IAgentDelegatedIdentityProvider>();
        uow.ClientsRepository.Returns(clients);
        uow.UsersRepository.Returns(users);
        uow.UserAccountsRepository.Returns(accounts);

        var user = new UserEntity("Ana Pérez", "ana@test.com", null, Guid.NewGuid());
        var client = new ClientEntity(user.Id, "1095914051", null);
        var account = new UserAccountEntity(user.Id, "ana", "ana@test.com", "Activo");

        clients.GetByIdentificationNumberAsync("1095914051", Arg.Any<CancellationToken>())
            .Returns(client);
        users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        accounts.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(account);
        identity.GetAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new AgentDelegatedIdentity(user.Id, "Cliente", "token-abc"));

        var handler = new FindOrCreateBotClientCommandHandler(
            uow,
            identity,
            Substitute.For<Microsoft.Extensions.Logging.ILogger<FindOrCreateBotClientCommandHandler>>());

        var result = await handler.Handle(
            new FindOrCreateBotClientCommand(
                "1095914051",
                "Ana Pérez",
                "ana@test.com",
                "3001112233"),
            CancellationToken.None);

        Assert.False(result.Created);
        Assert.Equal(client.Id, result.ClientId);
        Assert.Equal(account.Id, result.UserAccountId);
        Assert.Equal("token-abc", result.AccessToken);
        await clients.DidNotReceive().AddAsync(Arg.Any<ClientEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_name_is_rejected()
    {
        var handler = new FindOrCreateBotClientCommandHandler(
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IAgentDelegatedIdentityProvider>(),
            Substitute.For<Microsoft.Extensions.Logging.ILogger<FindOrCreateBotClientCommandHandler>>());

        await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(
                new FindOrCreateBotClientCommand("1095914051", "  ", "ana@test.com"),
                CancellationToken.None));
    }
}
