using Application.Telegram.Abstractions;
using Application.Telegram.Linking;
using Application.Telegram.Models;
using Application.Verification.Abstractions;
using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using Domain.Verification.Enums;
using NSubstitute;
using Xunit;

namespace Application.Tests.Telegram;

public sealed class TelegramChatLinkingServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 31, 20, 0, 0, TimeSpan.Zero);
    private static readonly Guid PersonId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task Vincular_starts_a_session_waiting_for_email()
    {
        var fixture = CreateFixture();
        var update = ProcessingUpdate(42, "/vincular");

        var outcome = await fixture.Service.HandleAsync(update, default);

        Assert.True(outcome.Consumed);
        Assert.Contains("correo", outcome.Reply!, StringComparison.OrdinalIgnoreCase);
        await fixture.Sessions.Received(1).AddAsync(
            Arg.Is<TelegramLinkingSession>(session =>
                session.Status == TelegramLinkingSessionStatus.AwaitingEmail),
            default);
    }

    [Fact]
    public async Task Email_input_is_redacted_and_sends_an_otp()
    {
        var fixture = CreateFixture();
        var session = TelegramLinkingSession.Start(1001, 1001, Now.UtcDateTime);
        var update = ProcessingUpdate(43, "cliente@huellitas.test");
        fixture.Sessions.GetActiveByTelegramUserIdAsync(1001, default).Returns(session);
        fixture.Accounts.FindActiveByEmailAsync("cliente@huellitas.test", default)
            .Returns(new TelegramLinkableAccount(PersonId, "cliente@huellitas.test"));

        var outcome = await fixture.Service.HandleAsync(update, default);

        Assert.True(outcome.Consumed);
        Assert.Null(update.MessageText);
        Assert.Equal(TelegramLinkingSessionStatus.AwaitingOtp, session.Status);
        await fixture.Sender.Received(1).SendAsync(
            VerificationDeliveryChannel.Email,
            "cliente@huellitas.test",
            "123456",
            Now.AddMinutes(5),
            default);
    }

    [Fact]
    public async Task Vincular_does_not_restart_a_recent_otp_session()
    {
        var fixture = CreateFixture();
        var session = OtpSession();
        var update = ProcessingUpdate(47, "/vincular");
        fixture.Sessions.GetActiveByTelegramUserIdAsync(1001, default).Returns(session);

        var outcome = await fixture.Service.HandleAsync(update, default);

        Assert.True(outcome.Consumed);
        Assert.Contains("enviado", outcome.Reply!, StringComparison.OrdinalIgnoreCase);
        await fixture.Sessions.DidNotReceive().AddAsync(
            Arg.Any<TelegramLinkingSession>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fifth_invalid_otp_blocks_the_session()
    {
        var fixture = CreateFixture();
        var session = OtpSession();
        for (var attempt = 0; attempt < 4; attempt++)
        {
            session.RegisterFailedAttempt(5, Now.UtcDateTime.AddSeconds(attempt + 1));
        }

        var update = ProcessingUpdate(44, "999999");
        fixture.Sessions.GetActiveByTelegramUserIdAsync(1001, default).Returns(session);
        fixture.Protector.Verify("999999", Hash).Returns(false);

        var outcome = await fixture.Service.HandleAsync(update, default);

        Assert.True(outcome.Consumed);
        Assert.Equal(TelegramLinkingSessionStatus.Blocked, session.Status);
        Assert.Null(update.MessageText);
    }

    [Fact]
    public async Task Valid_otp_creates_a_permanent_user_link()
    {
        var fixture = CreateFixture();
        var session = OtpSession();
        var update = ProcessingUpdate(45, "123456");
        fixture.Sessions.GetActiveByTelegramUserIdAsync(1001, default).Returns(session);
        fixture.Protector.Verify("123456", Hash).Returns(true);
        fixture.UserLinks.GetByPersonIdAsync(PersonId, default)
            .Returns((TelegramUserLink?)null);

        var outcome = await fixture.Service.HandleAsync(update, default);

        Assert.True(outcome.Consumed);
        Assert.Contains("vinculada", outcome.Reply!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(TelegramLinkingSessionStatus.Linked, session.Status);
        await fixture.UserLinks.Received(1).AddAsync(
            Arg.Is<TelegramUserLink>(link =>
                link.PersonId == PersonId && link.TelegramUserId == 1001),
            default);
    }

    [Fact]
    public async Task Valid_otp_creates_a_new_link_when_the_previous_chat_link_is_revoked()
    {
        var fixture = CreateFixture();
        var session = OtpSession();
        var update = ProcessingUpdate(49, "123456");
        var revoked = TelegramUserLink.Create(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            1001,
            1001,
            Now.UtcDateTime.AddMinutes(-2));
        revoked.Revoke(Now.UtcDateTime.AddMinutes(-1));
        fixture.Sessions.GetActiveByTelegramUserIdAsync(1001, default).Returns(session);
        fixture.Protector.Verify("123456", Hash).Returns(true);
        fixture.UserLinks.GetByPersonIdAsync(PersonId, default)
            .Returns((TelegramUserLink?)null);
        fixture.UserLinks.GetByTelegramChatIdAsync(1001, default)
            .Returns((TelegramUserLink?)null);

        var outcome = await fixture.Service.HandleAsync(update, default);

        Assert.True(outcome.Consumed);
        Assert.Equal(TelegramLinkingSessionStatus.Linked, session.Status);
        await fixture.UserLinks.Received(1).AddAsync(
            Arg.Is<TelegramUserLink>(link =>
                link.PersonId == PersonId && link.TelegramChatId == revoked.TelegramChatId),
            default);
    }

    [Fact]
    public async Task Valid_otp_rejects_a_chat_actively_linked_to_another_person()
    {
        var fixture = CreateFixture();
        var session = OtpSession();
        var update = ProcessingUpdate(50, "123456");
        var occupied = TelegramUserLink.Create(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            2002,
            1001,
            Now.UtcDateTime.AddMinutes(-1));
        fixture.Sessions.GetActiveByTelegramUserIdAsync(1001, default).Returns(session);
        fixture.Protector.Verify("123456", Hash).Returns(true);
        fixture.UserLinks.GetByPersonIdAsync(PersonId, default)
            .Returns((TelegramUserLink?)null);
        fixture.UserLinks.GetByTelegramChatIdAsync(1001, default).Returns(occupied);

        var outcome = await fixture.Service.HandleAsync(update, default);

        Assert.True(outcome.Consumed);
        Assert.Contains("vinculad", outcome.Reply!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(TelegramLinkingSessionStatus.Cancelled, session.Status);
        await fixture.UserLinks.DidNotReceive().AddAsync(
            Arg.Any<TelegramUserLink>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirmed_unlink_revokes_the_permanent_link()
    {
        var fixture = CreateFixture();
        var link = TelegramUserLink.Create(PersonId, 1001, 1001, Now.UtcDateTime);
        var update = ProcessingUpdate(46, "/desvincular confirmar");
        fixture.UserLinks.GetByTelegramUserIdAsync(1001, default).Returns(link);

        var outcome = await fixture.Service.HandleAsync(update, default);

        Assert.True(outcome.Consumed);
        Assert.False(link.IsActive);
        Assert.Contains("desvinculada", outcome.Reply!, StringComparison.OrdinalIgnoreCase);
    }

    private static Fixture CreateFixture()
    {
        var unitOfWork = Substitute.For<ITelegramUnitOfWork>();
        var sessions = Substitute.For<ITelegramLinkingSessionRepository>();
        var userLinks = Substitute.For<ITelegramUserLinkRepository>();
        var inboundUpdates = Substitute.For<ITelegramInboundUpdateRepository>();
        unitOfWork.LinkingSessionsRepository.Returns(sessions);
        unitOfWork.UserLinksRepository.Returns(userLinks);
        unitOfWork.InboundUpdatesRepository.Returns(inboundUpdates);
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
                call.ArgAt<Func<CancellationToken, Task>>(0)(
                    call.ArgAt<CancellationToken>(1)));
        var accounts = Substitute.For<ITelegramAccountLookup>();
        var sender = Substitute.For<IVerificationCodeDispatcher>();
        var protector = Substitute.For<IOtpProtector>();
        protector.Create().Returns(new GeneratedOtp("123456", Hash));
        protector.HashEmail(Arg.Any<string>()).Returns(Hash);
        var settings = Substitute.For<ITelegramRuntimeSettings>();
        settings.OtpLifetime.Returns(TimeSpan.FromMinutes(5));
        settings.OtpMaximumAttempts.Returns(5);
        settings.OtpResendInterval.Returns(TimeSpan.FromMinutes(1));
        var service = new TelegramChatLinkingService(
            unitOfWork,
            accounts,
            sender,
            protector,
            settings,
            new FixedTimeProvider(Now));
        return new Fixture(service, sessions, userLinks, accounts, sender, protector);
    }

    private static TelegramLinkingSession OtpSession()
    {
        var session = TelegramLinkingSession.Start(1001, 1001, Now.UtcDateTime);
        session.ResolveAccount(PersonId, Hash, Hash, Now.AddMinutes(5).UtcDateTime, Now.UtcDateTime);
        return session;
    }

    private static TelegramInboundUpdate ProcessingUpdate(long id, string text)
    {
        var update = TelegramInboundUpdate.Create(
            id,
            1001,
            1001,
            id,
            "private",
            text,
            Now.UtcDateTime);
        update.Claim(Now.UtcDateTime);
        return update;
    }

    private sealed record Fixture(
        TelegramChatLinkingService Service,
        ITelegramLinkingSessionRepository Sessions,
        ITelegramUserLinkRepository UserLinks,
        ITelegramAccountLookup Accounts,
        IVerificationCodeDispatcher Sender,
        IOtpProtector Protector);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
