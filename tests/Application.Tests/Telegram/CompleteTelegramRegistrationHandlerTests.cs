using Application.Common.Results;
using Application.Security.Errors;
using Application.Security.Registration;
using Application.Telegram.Abstractions;
using Application.Telegram.Registration;
using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using NSubstitute;
using Xunit;

namespace Application.Tests.Telegram;

public sealed class CompleteTelegramRegistrationHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid PersonId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AccountId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid RoleId =
        Guid.Parse("33333333-3333-3333-3333-333333333333");
    private const string Hash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public async Task Complete_stages_account_links_chat_and_consumes_session_in_one_transaction()
    {
        var fixture = CreateFixture();
        var session = ProfileSession();
        fixture.Sessions.GetByCompletionTokenHashAsync(Hash, default).Returns(session);
        fixture.Registration.StageAsync(Arg.Any<ClientAccountRegistrationRequest>(), default)
            .Returns(Result<RegisteredClientAccount>.Success(RegisteredAccount()));

        var result = await fixture.Handler.Handle(Command(), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(PersonId, result.Value.PersonId);
        await fixture.Links.Received(1).AddAsync(
            Arg.Is<TelegramUserLink>(link => link.PersonId == PersonId), default);
        Assert.Equal(TelegramRegistrationSessionStatus.Completed, session.Status);
    }

    [Fact]
    public async Task Reused_or_unknown_token_is_rejected_without_staging_account()
    {
        var fixture = CreateFixture();
        fixture.Sessions.GetByCompletionTokenHashAsync(Hash, default)
            .Returns((TelegramRegistrationSession?)null);

        var result = await fixture.Handler.Handle(Command(), default);

        Assert.True(result.IsFailure);
        Assert.Equal(TelegramRegistrationErrors.InvalidOrExpired, result.Error);
        await fixture.Registration.DidNotReceive().StageAsync(
            Arg.Any<ClientAccountRegistrationRequest>(), default);
    }

    [Fact]
    public async Task Registration_conflict_keeps_session_awaiting_profile()
    {
        var fixture = CreateFixture();
        var session = ProfileSession();
        fixture.Sessions.GetByCompletionTokenHashAsync(Hash, default).Returns(session);
        fixture.Registration.StageAsync(Arg.Any<ClientAccountRegistrationRequest>(), default)
            .Returns(Result<RegisteredClientAccount>.Failure(
                AuthenticationErrors.IdentificationNumberAlreadyExists));

        var result = await fixture.Handler.Handle(Command(), default);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthenticationErrors.IdentificationNumberAlreadyExists, result.Error);
        Assert.Equal(TelegramRegistrationSessionStatus.AwaitingProfile, session.Status);
        await fixture.Links.DidNotReceive().AddAsync(Arg.Any<TelegramUserLink>(), default);
    }

    private static Fixture CreateFixture()
    {
        var unitOfWork = Substitute.For<ITelegramUnitOfWork>();
        var sessions = Substitute.For<ITelegramRegistrationSessionRepository>();
        var links = Substitute.For<ITelegramUserLinkRepository>();
        unitOfWork.RegistrationSessionsRepository.Returns(sessions);
        unitOfWork.UserLinksRepository.Returns(links);
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(default));
        var protector = Substitute.For<ITelegramRegistrationProtector>();
        protector.HashCompletionToken("raw-token").Returns(Hash);
        protector.UnprotectEmail("protected-email").Returns("new@huellitas.test");
        var registration = Substitute.For<IClientAccountRegistrationService>();
        return new Fixture(
            new CompleteTelegramRegistrationCommandHandler(
                unitOfWork, protector, registration, new FixedTimeProvider(Now)),
            sessions, links, registration);
    }

    private static CompleteTelegramRegistrationCommand Command() => new(
        "raw-token", "Ana Cliente", "1234567890", "ana.cliente",
        "Password123!", "Password123!");

    private static TelegramRegistrationSession ProfileSession()
    {
        var session = TelegramRegistrationSession.Start(1001, 1001, Now.UtcDateTime);
        session.PrepareOtp(
            "protected-email", Hash, Hash, TelegramRegistrationAccountKind.New, null,
            Now.AddMinutes(10).UtcDateTime, Now.UtcDateTime);
        session.VerifyOtp(Now.AddMinutes(1).UtcDateTime);
        session.IssueCompletionToken(
            Hash, Now.AddMinutes(16).UtcDateTime, Now.AddMinutes(1).UtcDateTime);
        return session;
    }

    private static RegisteredClientAccount RegisteredAccount() => new(
        PersonId, AccountId, RoleId, "Cliente", "Ana Cliente",
        "ana.cliente", "new@huellitas.test", "Activo");

    private sealed record Fixture(
        CompleteTelegramRegistrationCommandHandler Handler,
        ITelegramRegistrationSessionRepository Sessions,
        ITelegramUserLinkRepository Links,
        IClientAccountRegistrationService Registration);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
