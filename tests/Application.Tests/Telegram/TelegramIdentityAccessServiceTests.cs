using Application.Telegram.Abstractions;
using Application.Telegram.Identity;
using Application.Telegram.Models;
using Application.Verification.Abstractions;
using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using Domain.Verification.Enums;
using NSubstitute;
using Xunit;

namespace Application.Tests.Telegram;

public sealed class TelegramIdentityAccessServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 4, 16, 0, 0, TimeSpan.Zero);
    private static readonly Guid PersonId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AccountId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task Private_access_without_link_starts_by_requesting_identification()
    {
        var fixture = CreateFixture();
        var update = ProcessingUpdate(42, "quiero ver mis mascotas");

        var outcome = await fixture.Service.BeginPrivateAccessAsync(update, default);

        Assert.True(outcome.Consumed);
        Assert.Contains("cédula", outcome.Reply!, StringComparison.OrdinalIgnoreCase);
        await fixture.Sessions.Received(1).AddAsync(
            Arg.Is<TelegramIdentitySession>(session =>
                session.Status == TelegramIdentitySessionStatus.AwaitingIdentification &&
                session.PendingInboundUpdateId == 42),
            default);
    }

    [Fact]
    public async Task Known_identification_is_redacted_and_receives_otp()
    {
        var fixture = CreateFixture();
        var session = TelegramIdentitySession.Start(1001, 1001, 42, Now.UtcDateTime);
        var update = ProcessingUpdate(43, "123456789");
        fixture.Sessions.GetCurrentByTelegramUserIdAsync(1001, default).Returns(session);
        fixture.Clients.FindActiveByIdentificationAsync("123456789", default)
            .Returns(new TelegramClientIdentity(PersonId, AccountId, "cliente@huellitas.test"));

        var outcome = await fixture.Service.HandleActiveFlowAsync(update, default);

        Assert.True(outcome.Consumed);
        Assert.Null(update.MessageText);
        Assert.Equal(TelegramIdentitySessionStatus.AwaitingOtp, session.Status);
        await fixture.Sender.Received(1).SendAsync(
            VerificationDeliveryChannel.Email,
            "cliente@huellitas.test",
            "123456",
            Now.AddMinutes(5),
            default);
    }

    [Fact]
    public async Task Unknown_identification_collects_minimum_registration_data()
    {
        var fixture = CreateFixture();
        var session = TelegramIdentitySession.Start(1001, 1001, 42, Now.UtcDateTime);
        fixture.Sessions.GetCurrentByTelegramUserIdAsync(1001, default).Returns(session);
        fixture.Clients.FindActiveByIdentificationAsync("999999999", default)
            .Returns((TelegramClientIdentity?)null);

        await fixture.Service.HandleActiveFlowAsync(
            ProcessingUpdate(43, "999999999"), default);
        await fixture.Service.HandleActiveFlowAsync(
            ProcessingUpdate(44, "sí"), default);
        await fixture.Service.HandleActiveFlowAsync(
            ProcessingUpdate(45, "Ana Pérez"), default);
        var outcome = await fixture.Service.HandleActiveFlowAsync(
            ProcessingUpdate(46, "ana@example.test"), default);

        Assert.True(outcome.Consumed);
        Assert.Equal(TelegramIdentitySessionStatus.AwaitingOtp, session.Status);
        Assert.Equal("protected:identification:999999999", session.ProtectedIdentification);
        Assert.Equal("protected:full-name:Ana Pérez", session.ProtectedFullName);
        Assert.Equal("protected:email:ana@example.test", session.ProtectedEmail);
        await fixture.Sender.Received(1).SendAsync(
            VerificationDeliveryChannel.Email,
            "ana@example.test",
            "123456",
            Now.AddMinutes(5),
            default);
    }

    [Fact]
    public async Task Valid_known_client_otp_links_identity_and_returns_pending_update()
    {
        var fixture = CreateFixture();
        var session = KnownOtpSession();
        fixture.Sessions.GetCurrentByTelegramUserIdAsync(1001, default).Returns(session);
        fixture.Otp.Verify("123456", Hash).Returns(true);

        var outcome = await fixture.Service.HandleActiveFlowAsync(
            ProcessingUpdate(44, "123456"), default);

        Assert.Equal(PersonId, outcome.VerifiedPersonId);
        Assert.Equal(42, outcome.ResumeInboundUpdateId);
        Assert.Equal("quiero ver mis mascotas", outcome.ResumeMessage);
        Assert.True(session.IsAccessValid(Now.AddMinutes(29).UtcDateTime));
        await fixture.UserLinks.Received(1).AddAsync(
            Arg.Is<TelegramUserLink>(link => link.PersonId == PersonId),
            default);
    }

    [Fact]
    public async Task Valid_registration_otp_stages_client_and_verifies_access_atomically()
    {
        var fixture = CreateFixture();
        var session = RegistrationOtpSession();
        fixture.Sessions.GetCurrentByTelegramUserIdAsync(1001, default).Returns(session);
        fixture.Otp.Verify("123456", Hash).Returns(true);
        fixture.Clients.StageRegistrationAsync(
                Arg.Any<TelegramClientRegistration>(),
                default)
            .Returns(new TelegramClientIdentity(PersonId, AccountId, "ana@example.test"));

        var outcome = await fixture.Service.HandleActiveFlowAsync(
            ProcessingUpdate(47, "123456"), default);

        Assert.Equal(PersonId, outcome.VerifiedPersonId);
        Assert.Equal(42, outcome.ResumeInboundUpdateId);
        Assert.Equal("quiero ver mis mascotas", outcome.ResumeMessage);
        await fixture.Clients.Received(1).StageRegistrationAsync(
            new TelegramClientRegistration("999999999", "Ana Pérez", "ana@example.test"),
            default);
        await fixture.UnitOfWork.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task>>(),
            default);
    }

    [Fact]
    public async Task Invalid_otp_blocks_at_the_configured_attempt_limit()
    {
        var fixture = CreateFixture(maximumAttempts: 1);
        var session = KnownOtpSession();
        fixture.Sessions.GetCurrentByTelegramUserIdAsync(1001, default).Returns(session);
        fixture.Otp.Verify("000000", Hash).Returns(false);

        var outcome = await fixture.Service.HandleActiveFlowAsync(
            ProcessingUpdate(44, "000000"), default);

        Assert.Equal(TelegramIdentitySessionStatus.Blocked, session.Status);
        Assert.Contains("intentos", outcome.Reply!, StringComparison.OrdinalIgnoreCase);
    }

    private static Fixture CreateFixture(int maximumAttempts = 5)
    {
        var unitOfWork = Substitute.For<ITelegramUnitOfWork>();
        var sessions = Substitute.For<ITelegramIdentitySessionRepository>();
        var userLinks = Substitute.For<ITelegramUserLinkRepository>();
        var inboundUpdates = Substitute.For<ITelegramInboundUpdateRepository>();
        unitOfWork.IdentitySessionsRepository.Returns(sessions);
        unitOfWork.UserLinksRepository.Returns(userLinks);
        unitOfWork.InboundUpdatesRepository.Returns(inboundUpdates);
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
                call.ArgAt<Func<CancellationToken, Task>>(0)(
                    call.ArgAt<CancellationToken>(1)));

        var clients = Substitute.For<ITelegramClientIdentityGateway>();
        var sender = Substitute.For<IVerificationCodeDispatcher>();
        var otp = Substitute.For<IOtpProtector>();
        otp.Create().Returns(new GeneratedOtp("123456", Hash));
        var protector = Substitute.For<ITelegramIdentityDataProtector>();
        protector.Protect(Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => $"protected:{call.ArgAt<string>(0)}:{call.ArgAt<string>(1)}");
        protector.Unprotect(Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => call.ArgAt<string>(1).Split(':', 3)[2]);
        var settings = Substitute.For<ITelegramRuntimeSettings>();
        settings.OtpLifetime.Returns(TimeSpan.FromMinutes(5));
        settings.OtpMaximumAttempts.Returns(maximumAttempts);
        settings.PrivateAccessAbsoluteLifetime.Returns(TimeSpan.FromHours(24));
        settings.PrivateAccessIdleLifetime.Returns(TimeSpan.FromMinutes(30));
        var service = new TelegramIdentityAccessService(
            unitOfWork,
            clients,
            sender,
            otp,
            protector,
            settings,
            new FixedTimeProvider(Now));
        return new Fixture(service, unitOfWork, sessions, userLinks, clients, sender, otp);
    }

    private static TelegramIdentitySession KnownOtpSession()
    {
        var session = TelegramIdentitySession.Start(1001, 1001, 42, Now.UtcDateTime);
        session.CapturePendingMessage(
            "protected:pending-message:quiero ver mis mascotas",
            Now.UtcDateTime);
        session.BeginKnownClientOtp(PersonId, Hash, Now.AddMinutes(5).UtcDateTime, Now.UtcDateTime);
        return session;
    }

    private static TelegramIdentitySession RegistrationOtpSession()
    {
        var session = TelegramIdentitySession.Start(1001, 1001, 42, Now.UtcDateTime);
        session.CapturePendingMessage(
            "protected:pending-message:quiero ver mis mascotas",
            Now.UtcDateTime);
        session.RequireRegistration("protected:identification:999999999", Now.UtcDateTime);
        session.ConfirmRegistration(Now.UtcDateTime);
        session.CaptureFullName("protected:full-name:Ana Pérez", Now.UtcDateTime);
        session.BeginRegistrationOtp(
            "protected:email:ana@example.test",
            Hash,
            Now.AddMinutes(5).UtcDateTime,
            Now.UtcDateTime);
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
        TelegramIdentityAccessService Service,
        ITelegramUnitOfWork UnitOfWork,
        ITelegramIdentitySessionRepository Sessions,
        ITelegramUserLinkRepository UserLinks,
        ITelegramClientIdentityGateway Clients,
        IVerificationCodeDispatcher Sender,
        IOtpProtector Otp);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
