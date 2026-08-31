using Application.Telegram.Abstractions;
using Application.Telegram.Linking;
using Application.Telegram.Updates;
using Application.Users.Abstraction;
using Domain.Telegram.Entities;
using NSubstitute;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Telegram;

public sealed class TelegramApplicationHandlersTests
{
    private static readonly Guid PersonId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Now =
        new(2026, 8, 31, 18, 0, 0, TimeSpan.Zero);
    private const string RawCode = "telegram-link-code";
    private const string CodeHash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task Create_code_returns_raw_value_but_persists_only_hash()
    {
        var fixture = CreateFixture();
        fixture.Users.GetByIdAsync(PersonId, fixture.Token)
            .Returns(new UserEntity("Samuel", "samuel@example.com", "hash", Guid.NewGuid()));
        fixture.Protector.Create().Returns(new TelegramProtectedCode(RawCode, CodeHash));
        var handler = new CreateTelegramLinkCodeHandler(
            fixture.UnitOfWork,
            fixture.Protector,
            fixture.Settings,
            fixture.TimeProvider);

        var result = await handler.Handle(
            new CreateTelegramLinkCodeCommand(PersonId),
            fixture.Token);

        Assert.Equal(RawCode, result.Code);
        Assert.Equal("https://t.me/HuellitasBot?start=telegram-link-code", result.DeepLink);
        await fixture.LinkCodes.Received(1).AddAsync(
            Arg.Is<TelegramLinkCode>(code =>
                code.PersonId == PersonId &&
                code.CodeHash == CodeHash &&
                code.CodeHash != RawCode),
            fixture.Token);
        await fixture.UnitOfWork.Received(1).SaveChangesAsync(fixture.Token);
    }

    [Fact]
    public async Task Consume_code_relinks_existing_person_and_consumes_code()
    {
        var fixture = CreateFixture();
        var code = TelegramLinkCode.Create(
            PersonId,
            CodeHash,
            Now.UtcDateTime.AddMinutes(10),
            Now.UtcDateTime);
        var existing = TelegramUserLink.Create(PersonId, 1001, 1001, Now.UtcDateTime);
        fixture.Protector.Hash(RawCode).Returns(CodeHash);
        fixture.LinkCodes.GetActiveByHashAsync(CodeHash, Now.UtcDateTime, fixture.Token)
            .Returns(code);
        fixture.Users.GetByIdAsync(PersonId, fixture.Token)
            .Returns(new UserEntity("Samuel", "samuel@example.com", "hash", Guid.NewGuid()));
        fixture.UserLinks.GetByTelegramUserIdAsync(2002, fixture.Token)
            .Returns((TelegramUserLink?)null);
        fixture.UserLinks.GetByPersonIdAsync(PersonId, fixture.Token)
            .Returns(existing);
        var handler = new ConsumeTelegramLinkCodeHandler(
            fixture.UnitOfWork,
            fixture.Protector,
            fixture.TimeProvider);

        var result = await handler.Handle(
            new ConsumeTelegramLinkCodeCommand(RawCode, 2002, 2002),
            fixture.Token);

        Assert.Equal(existing.Id, result);
        Assert.Equal(2002, existing.TelegramUserId);
        Assert.NotNull(code.ConsumedAt);
        await fixture.UnitOfWork.Received(1).SaveChangesAsync(fixture.Token);
    }

    [Fact]
    public async Task Ingest_duplicate_update_does_not_add_a_second_work_item()
    {
        var fixture = CreateFixture();
        fixture.InboundUpdates.ExistsAsync(42, fixture.Token).Returns(true);
        var handler = new IngestTelegramUpdateHandler(fixture.UnitOfWork, fixture.TimeProvider);

        var result = await handler.Handle(
            new IngestTelegramUpdateCommand(42, 1001, 1001, 7, "private", "hola"),
            fixture.Token);

        Assert.Equal(IngestTelegramUpdateResult.Duplicate, result);
        await fixture.InboundUpdates.DidNotReceive().AddAsync(
            Arg.Any<TelegramInboundUpdate>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Ingest_new_update_creates_pending_work_item()
    {
        var fixture = CreateFixture();
        fixture.InboundUpdates.ExistsAsync(42, fixture.Token).Returns(false);
        var handler = new IngestTelegramUpdateHandler(fixture.UnitOfWork, fixture.TimeProvider);

        var result = await handler.Handle(
            new IngestTelegramUpdateCommand(42, 1001, 1001, 7, "private", "hola"),
            fixture.Token);

        Assert.Equal(IngestTelegramUpdateResult.Accepted, result);
        await fixture.InboundUpdates.Received(1).AddAsync(
            Arg.Is<TelegramInboundUpdate>(update =>
                update.Id == 42 && update.MessageText == "hola"),
            fixture.Token);
        await fixture.UnitOfWork.Received(1).SaveChangesAsync(fixture.Token);
    }

    private static Fixture CreateFixture()
    {
        var unitOfWork = Substitute.For<ITelegramUnitOfWork>();
        var users = Substitute.For<IUsersRepository>();
        var linkCodes = Substitute.For<ITelegramLinkCodeRepository>();
        var userLinks = Substitute.For<ITelegramUserLinkRepository>();
        var conversationLinks = Substitute.For<ITelegramConversationLinkRepository>();
        var inboundUpdates = Substitute.For<ITelegramInboundUpdateRepository>();
        var protector = Substitute.For<ITelegramLinkCodeProtector>();
        var settings = Substitute.For<ITelegramRuntimeSettings>();
        settings.BotUsername.Returns("HuellitasBot");
        settings.LinkCodeTtl.Returns(TimeSpan.FromMinutes(10));
        unitOfWork.UsersRepository.Returns(users);
        unitOfWork.LinkCodesRepository.Returns(linkCodes);
        unitOfWork.UserLinksRepository.Returns(userLinks);
        unitOfWork.ConversationLinksRepository.Returns(conversationLinks);
        unitOfWork.InboundUpdatesRepository.Returns(inboundUpdates);

        return new Fixture(
            unitOfWork,
            users,
            linkCodes,
            userLinks,
            inboundUpdates,
            protector,
            settings,
            new FixedTimeProvider(Now),
            CancellationToken.None);
    }

    private sealed record Fixture(
        ITelegramUnitOfWork UnitOfWork,
        IUsersRepository Users,
        ITelegramLinkCodeRepository LinkCodes,
        ITelegramUserLinkRepository UserLinks,
        ITelegramInboundUpdateRepository InboundUpdates,
        ITelegramLinkCodeProtector Protector,
        ITelegramRuntimeSettings Settings,
        TimeProvider TimeProvider,
        CancellationToken Token);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
