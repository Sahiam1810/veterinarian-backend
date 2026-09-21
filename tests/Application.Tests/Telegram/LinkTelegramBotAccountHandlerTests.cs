using Application.Clients.Abstraction;
using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Application.Telegram.Linking;
using Domain.Clients.Entities;
using Domain.Telegram.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Telegram;

public sealed class LinkTelegramBotAccountHandlerTests
{
    private static readonly Guid ClientId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Now =
        new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);
    private const long TelegramUserId = 555;

    [Fact]
    public async Task Creates_link_when_client_exists_and_not_linked()
    {
        var fixture = CreateFixture();
        var client = CreateClient("Ana Dueña", "ana@huellitas.test");
        fixture.Clients.GetByIdAsync(client.Id, fixture.Token).Returns(client);
        fixture.UserLinks.GetByTelegramUserIdAsync(TelegramUserId, fixture.Token)
            .Returns((TelegramUserLink?)null);
        fixture.UserLinks.GetByClientIdAsync(client.Id, fixture.Token)
            .Returns((TelegramUserLink?)null);
        var handler = new LinkTelegramBotAccountHandler(fixture.UnitOfWork, fixture.TimeProvider);

        var linkId = await handler.Handle(
            new LinkTelegramBotAccountCommand(client.Id, TelegramUserId),
            fixture.Token);

        await fixture.UserLinks.Received(1).AddAsync(
            Arg.Is<TelegramUserLink>(link =>
                link.ClientId == client.Id &&
                link.TelegramUserId == TelegramUserId &&
                link.TelegramChatId == TelegramUserId),
            fixture.Token);
        await fixture.UnitOfWork.Received(1).SaveChangesAsync(fixture.Token);
        Assert.NotEqual(Guid.Empty, linkId);
    }

    [Fact]
    public async Task Linking_the_same_client_twice_is_idempotent()
    {
        var fixture = CreateFixture();
        var client = CreateClient("Ana Dueña", "ana@huellitas.test");
        var existingLink = TelegramUserLink.Create(
            client.Id, TelegramUserId, TelegramUserId, Now.UtcDateTime);
        fixture.Clients.GetByIdAsync(client.Id, fixture.Token).Returns(client);
        fixture.UserLinks.GetByTelegramUserIdAsync(TelegramUserId, fixture.Token)
            .Returns(existingLink);
        var handler = new LinkTelegramBotAccountHandler(fixture.UnitOfWork, fixture.TimeProvider);

        var linkId = await handler.Handle(
            new LinkTelegramBotAccountCommand(client.Id, TelegramUserId),
            fixture.Token);

        Assert.Equal(existingLink.Id, linkId);
        await fixture.UserLinks.DidNotReceive().AddAsync(
            Arg.Any<TelegramUserLink>(), Arg.Any<CancellationToken>());
        await fixture.UserLinks.DidNotReceive().UpdateAsync(
            Arg.Any<TelegramUserLink>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Telegram_user_already_linked_to_a_different_client_is_a_conflict()
    {
        var fixture = CreateFixture();
        var client = CreateClient("Ana Dueña", "ana@huellitas.test");
        var otherClientId = Guid.NewGuid();
        var conflictingLink = TelegramUserLink.Create(
            otherClientId, TelegramUserId, TelegramUserId, Now.UtcDateTime);
        fixture.Clients.GetByIdAsync(client.Id, fixture.Token).Returns(client);
        fixture.UserLinks.GetByTelegramUserIdAsync(TelegramUserId, fixture.Token)
            .Returns(conflictingLink);
        var handler = new LinkTelegramBotAccountHandler(fixture.UnitOfWork, fixture.TimeProvider);

        await Assert.ThrowsAsync<TelegramIdentityConflictException>(() => handler.Handle(
            new LinkTelegramBotAccountCommand(client.Id, TelegramUserId),
            fixture.Token));
    }

    [Fact]
    public async Task Missing_or_inactive_client_is_reported_as_account_unavailable()
    {
        var fixture = CreateFixture();
        fixture.Clients.GetByIdAsync(ClientId, fixture.Token).Returns((ClientEntity?)null);
        var handler = new LinkTelegramBotAccountHandler(fixture.UnitOfWork, fixture.TimeProvider);

        await Assert.ThrowsAsync<TelegramAccountUnavailableException>(() => handler.Handle(
            new LinkTelegramBotAccountCommand(ClientId, TelegramUserId),
            fixture.Token));
    }

    private static ClientEntity CreateClient(string name, string email)
    {
        return new ClientEntity(Guid.NewGuid(), name, email, "1234567890", "3001234567", "Calle 1");
    }

    private static Fixture CreateFixture()
    {
        var unitOfWork = Substitute.For<ITelegramUnitOfWork>();
        var clients = Substitute.For<IClientRepository>();
        var userLinks = Substitute.For<ITelegramUserLinkRepository>();
        unitOfWork.ClientsRepository.Returns(clients);
        unitOfWork.UserLinksRepository.Returns(userLinks);

        return new Fixture(
            unitOfWork,
            clients,
            userLinks,
            new FixedTimeProvider(Now),
            CancellationToken.None);
    }

    private sealed record Fixture(
        ITelegramUnitOfWork UnitOfWork,
        IClientRepository Clients,
        ITelegramUserLinkRepository UserLinks,
        TimeProvider TimeProvider,
        CancellationToken Token);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
