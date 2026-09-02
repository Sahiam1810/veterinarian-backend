using Application.Telegram.Abstractions;
using Application.Telegram.Models;
using Application.Telegram.Registration;
using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using NSubstitute;
using Xunit;

namespace Application.Tests.Telegram;

public sealed class TelegramRegistrationServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid PersonId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string Hash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string TokenHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public async Task Register_command_starts_session_for_unlinked_private_chat()
    {
        var fixture = CreateFixture();

        var outcome = await fixture.Service.HandleAsync(Update(1, "/registrar"), default);

        Assert.True(outcome.Consumed);
        Assert.Contains("correo", outcome.Reply!, StringComparison.OrdinalIgnoreCase);
        await fixture.Sessions.Received(1).AddAsync(
            Arg.Is<TelegramRegistrationSession>(session =>
                session.Status == TelegramRegistrationSessionStatus.AwaitingEmail),
            default);
    }

    [Fact]
    public async Task Email_submission_sends_otp_and_redacts_update()
    {
        var fixture = CreateFixture();
        var session = TelegramRegistrationSession.Start(1001, 1001, Now.UtcDateTime);
        fixture.Sessions.GetActiveByTelegramUserIdAsync(1001, default).Returns(session);
        fixture.Accounts.FindByEmailAsync("new@huellitas.test", default)
            .Returns(new TelegramRegistrationAccount(
                TelegramRegistrationAccountKind.New, null, "new@huellitas.test"));
        var update = Update(2, "new@huellitas.test");

        var outcome = await fixture.Service.HandleAsync(update, default);

        Assert.True(outcome.Consumed);
        Assert.Null(update.MessageText);
        await fixture.Sender.Received(1).SendAsync(
            "new@huellitas.test", "123456", Now.AddMinutes(10), default);
        Assert.Equal(TelegramRegistrationSessionStatus.AwaitingOtp, session.Status);
    }

    [Fact]
    public async Task Valid_otp_for_new_email_returns_single_use_completion_link()
    {
        var fixture = CreateFixture();
        var session = OtpSession(TelegramRegistrationAccountKind.New);
        fixture.Sessions.GetActiveByTelegramUserIdAsync(1001, default).Returns(session);
        fixture.Otp.Verify("123456", Hash).Returns(true);

        var outcome = await fixture.Service.HandleAsync(Update(3, "123456"), default);

        Assert.True(outcome.Consumed);
        Assert.Contains("https://registration.test/telegram/registration/complete?token=raw-token", outcome.Reply!);
        Assert.Equal(TelegramRegistrationSessionStatus.AwaitingProfile, session.Status);
        Assert.Equal(TokenHash, session.CompletionTokenHash);
    }

    [Fact]
    public async Task Valid_otp_for_active_account_links_chat_without_profile_form()
    {
        var fixture = CreateFixture();
        var session = OtpSession(TelegramRegistrationAccountKind.Active, PersonId);
        fixture.Sessions.GetActiveByTelegramUserIdAsync(1001, default).Returns(session);
        fixture.Otp.Verify("123456", Hash).Returns(true);

        var outcome = await fixture.Service.HandleAsync(Update(4, "123456"), default);

        Assert.True(outcome.Consumed);
        Assert.Contains("vinculada", outcome.Reply!, StringComparison.OrdinalIgnoreCase);
        await fixture.Links.Received(1).AddAsync(
            Arg.Is<TelegramUserLink>(link => link.PersonId == PersonId), default);
        Assert.Equal(TelegramRegistrationSessionStatus.Completed, session.Status);
    }

    private static Fixture CreateFixture()
    {
        var unitOfWork = Substitute.For<ITelegramUnitOfWork>();
        var sessions = Substitute.For<ITelegramRegistrationSessionRepository>();
        var links = Substitute.For<ITelegramUserLinkRepository>();
        var inbound = Substitute.For<ITelegramInboundUpdateRepository>();
        unitOfWork.RegistrationSessionsRepository.Returns(sessions);
        unitOfWork.UserLinksRepository.Returns(links);
        unitOfWork.InboundUpdatesRepository.Returns(inbound);
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(default));

        var accounts = Substitute.For<ITelegramRegistrationAccountLookup>();
        var sender = Substitute.For<ITelegramVerificationCodeSender>();
        var otp = Substitute.For<ITelegramOtpProtector>();
        otp.Create().Returns(new GeneratedTelegramOtp("123456", Hash));
        otp.HashEmail(Arg.Any<string>()).Returns(Hash);
        var protector = Substitute.For<ITelegramRegistrationProtector>();
        protector.ProtectEmail(Arg.Any<string>()).Returns("protected-email");
        protector.GenerateCompletionToken().Returns("raw-token");
        protector.HashCompletionToken("raw-token").Returns(TokenHash);
        var settings = Substitute.For<ITelegramRuntimeSettings>();
        settings.RegistrationEnabled.Returns(true);
        settings.RegistrationCompletionUrl.Returns(
            "https://registration.test/telegram/registration/complete");
        settings.RegistrationOtpLifetime.Returns(TimeSpan.FromMinutes(10));
        settings.RegistrationTokenLifetime.Returns(TimeSpan.FromMinutes(15));
        settings.RegistrationMaximumOtpAttempts.Returns(3);
        settings.RegistrationResendInterval.Returns(TimeSpan.FromMinutes(1));

        return new Fixture(
            new TelegramRegistrationService(
                unitOfWork, accounts, sender, otp, protector, settings,
                new FixedTimeProvider(Now)),
            sessions, links, accounts, sender, otp);
    }

    private static TelegramRegistrationSession OtpSession(
        TelegramRegistrationAccountKind kind,
        Guid? personId = null)
    {
        var session = TelegramRegistrationSession.Start(1001, 1001, Now.UtcDateTime);
        session.PrepareOtp(
            "protected-email", Hash, Hash, kind, personId,
            Now.AddMinutes(10).UtcDateTime, Now.UtcDateTime);
        return session;
    }

    private static TelegramInboundUpdate Update(long id, string text)
    {
        var update = TelegramInboundUpdate.Create(id, 1001, 1001, id, "private", text, Now.UtcDateTime);
        update.Claim(Now.UtcDateTime);
        return update;
    }

    private sealed record Fixture(
        TelegramRegistrationService Service,
        ITelegramRegistrationSessionRepository Sessions,
        ITelegramUserLinkRepository Links,
        ITelegramRegistrationAccountLookup Accounts,
        ITelegramVerificationCodeSender Sender,
        ITelegramOtpProtector Otp);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
