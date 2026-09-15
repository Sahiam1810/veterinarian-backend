using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Application.Telegram.Linking;
using Application.UserAccounts.Abstraction;
using Application.Users.Abstraction;
using Domain.Telegram.Entities;
using NSubstitute;
using Xunit;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Telegram;

public sealed class LinkTelegramBotAccountHandlerTests
{
    private static readonly Guid PersonId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Now =
        new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);
    private const long TelegramUserId = 555;

    [Fact]
    public async Task Creates_ghost_account_and_link_when_neither_exists()
    {
        var fixture = CreateFixture();
        var user = new UserEntity("Ana Dueña", "ana@huellitas.test", null, Guid.NewGuid());
        fixture.Users.GetByIdAsync(user.Id, fixture.Token).Returns(user);
        fixture.Accounts.GetByUserIdAsync(user.Id, fixture.Token)
            .Returns((UserAccountEntity?)null);
        fixture.UserLinks.GetByTelegramUserIdAsync(TelegramUserId, fixture.Token)
            .Returns((TelegramUserLink?)null);
        fixture.UserLinks.GetByPersonIdAsync(user.Id, fixture.Token)
            .Returns((TelegramUserLink?)null);
        var handler = new LinkTelegramBotAccountHandler(fixture.UnitOfWork, fixture.TimeProvider);

        var linkId = await handler.Handle(
            new LinkTelegramBotAccountCommand(user.Id, TelegramUserId),
            fixture.Token);

        await fixture.Accounts.Received(1).AddAsync(
            Arg.Is<UserAccountEntity>(account =>
                account.UserId == user.Id &&
                account.Status == "Activo" &&
                account.Mail.Value == "ana@huellitas.test"),
            fixture.Token);
        await fixture.UserLinks.Received(1).AddAsync(
            Arg.Is<TelegramUserLink>(link =>
                link.PersonId == user.Id &&
                link.TelegramUserId == TelegramUserId &&
                link.TelegramChatId == TelegramUserId),
            fixture.Token);
        await fixture.UnitOfWork.Received(1).SaveChangesAsync(fixture.Token);
        Assert.NotEqual(Guid.Empty, linkId);
    }

    [Fact]
    public async Task Does_not_create_a_second_account_when_one_already_exists()
    {
        var fixture = CreateFixture();
        var user = new UserEntity("Ana Dueña", "ana@huellitas.test", null, Guid.NewGuid());
        var existingAccount = new UserAccountEntity(
            user.Id, "ana.owner", "ana@huellitas.test", "Activo");
        fixture.Users.GetByIdAsync(user.Id, fixture.Token).Returns(user);
        fixture.Accounts.GetByUserIdAsync(user.Id, fixture.Token).Returns(existingAccount);
        fixture.UserLinks.GetByTelegramUserIdAsync(TelegramUserId, fixture.Token)
            .Returns((TelegramUserLink?)null);
        fixture.UserLinks.GetByPersonIdAsync(user.Id, fixture.Token)
            .Returns((TelegramUserLink?)null);
        var handler = new LinkTelegramBotAccountHandler(fixture.UnitOfWork, fixture.TimeProvider);

        await handler.Handle(
            new LinkTelegramBotAccountCommand(user.Id, TelegramUserId),
            fixture.Token);

        await fixture.Accounts.DidNotReceive().AddAsync(
            Arg.Any<UserAccountEntity>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Linking_the_same_person_twice_is_idempotent()
    {
        var fixture = CreateFixture();
        var user = new UserEntity("Ana Dueña", "ana@huellitas.test", null, Guid.NewGuid());
        var account = new UserAccountEntity(user.Id, "tg_abc", "ana@huellitas.test", "Activo");
        var existingLink = TelegramUserLink.Create(
            user.Id, TelegramUserId, TelegramUserId, Now.UtcDateTime);
        fixture.Users.GetByIdAsync(user.Id, fixture.Token).Returns(user);
        fixture.Accounts.GetByUserIdAsync(user.Id, fixture.Token).Returns(account);
        fixture.UserLinks.GetByTelegramUserIdAsync(TelegramUserId, fixture.Token)
            .Returns(existingLink);
        var handler = new LinkTelegramBotAccountHandler(fixture.UnitOfWork, fixture.TimeProvider);

        var linkId = await handler.Handle(
            new LinkTelegramBotAccountCommand(user.Id, TelegramUserId),
            fixture.Token);

        Assert.Equal(existingLink.Id, linkId);
        await fixture.UserLinks.DidNotReceive().AddAsync(
            Arg.Any<TelegramUserLink>(), Arg.Any<CancellationToken>());
        await fixture.UserLinks.DidNotReceive().UpdateAsync(
            Arg.Any<TelegramUserLink>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Telegram_user_already_linked_to_a_different_person_is_a_conflict()
    {
        var fixture = CreateFixture();
        var user = new UserEntity("Ana Dueña", "ana@huellitas.test", null, Guid.NewGuid());
        var otherPersonId = Guid.NewGuid();
        var conflictingLink = TelegramUserLink.Create(
            otherPersonId, TelegramUserId, TelegramUserId, Now.UtcDateTime);
        fixture.Users.GetByIdAsync(user.Id, fixture.Token).Returns(user);
        fixture.UserLinks.GetByTelegramUserIdAsync(TelegramUserId, fixture.Token)
            .Returns(conflictingLink);
        var handler = new LinkTelegramBotAccountHandler(fixture.UnitOfWork, fixture.TimeProvider);

        await Assert.ThrowsAsync<TelegramIdentityConflictException>(() => handler.Handle(
            new LinkTelegramBotAccountCommand(user.Id, TelegramUserId),
            fixture.Token));
    }

    [Fact]
    public async Task Missing_or_inactive_user_is_reported_as_account_unavailable()
    {
        var fixture = CreateFixture();
        fixture.Users.GetByIdAsync(PersonId, fixture.Token).Returns((UserEntity?)null);
        var handler = new LinkTelegramBotAccountHandler(fixture.UnitOfWork, fixture.TimeProvider);

        await Assert.ThrowsAsync<TelegramAccountUnavailableException>(() => handler.Handle(
            new LinkTelegramBotAccountCommand(PersonId, TelegramUserId),
            fixture.Token));
    }

    private static Fixture CreateFixture()
    {
        var unitOfWork = Substitute.For<ITelegramUnitOfWork>();
        var users = Substitute.For<IUsersRepository>();
        var accounts = Substitute.For<IUserAccountsRepository>();
        var userLinks = Substitute.For<ITelegramUserLinkRepository>();
        unitOfWork.UsersRepository.Returns(users);
        unitOfWork.UserAccountsRepository.Returns(accounts);
        unitOfWork.UserLinksRepository.Returns(userLinks);
        accounts.ExistsByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        return new Fixture(
            unitOfWork,
            users,
            accounts,
            userLinks,
            new FixedTimeProvider(Now),
            CancellationToken.None);
    }

    private sealed record Fixture(
        ITelegramUnitOfWork UnitOfWork,
        IUsersRepository Users,
        IUserAccountsRepository Accounts,
        ITelegramUserLinkRepository UserLinks,
        TimeProvider TimeProvider,
        CancellationToken Token);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
