using Application.Telegram.Abstractions;
using Application.Telegram.Updates;
using Domain.Telegram.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Telegram;

public sealed class TelegramApplicationHandlersTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 31, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Ingest_duplicate_update_does_not_add_a_second_work_item()
    {
        var fixture = CreateFixture();
        fixture.InboundUpdates.ExistsAsync(42, fixture.Token).Returns(true);
        var handler = new IngestTelegramUpdateHandler(
            fixture.UnitOfWork,
            fixture.Signal,
            fixture.TimeProvider);

        var result = await handler.Handle(
            new IngestTelegramUpdateCommand(42, 1001, 1001, 7, "private", "hola"),
            fixture.Token);

        Assert.Equal(IngestTelegramUpdateResult.Duplicate, result);
        await fixture.InboundUpdates.DidNotReceive().AddAsync(
            Arg.Any<TelegramInboundUpdate>(),
            Arg.Any<CancellationToken>());
        fixture.Signal.DidNotReceive().Notify();
    }

    [Fact]
    public async Task Ingest_new_update_creates_pending_work_item()
    {
        var fixture = CreateFixture();
        fixture.InboundUpdates.ExistsAsync(42, fixture.Token).Returns(false);
        var handler = new IngestTelegramUpdateHandler(
            fixture.UnitOfWork,
            fixture.Signal,
            fixture.TimeProvider);

        var result = await handler.Handle(
            new IngestTelegramUpdateCommand(42, 1001, 1001, 7, "private", "hola"),
            fixture.Token);

        Assert.Equal(IngestTelegramUpdateResult.Accepted, result);
        await fixture.InboundUpdates.Received(1).AddAsync(
            Arg.Is<TelegramInboundUpdate>(update =>
                update.Id == 42 && update.MessageText == "hola"),
            fixture.Token);
        await fixture.UnitOfWork.Received(1).SaveChangesAsync(fixture.Token);
        fixture.Signal.Received(1).Notify();
    }

    private static Fixture CreateFixture()
    {
        var unitOfWork = Substitute.For<ITelegramUnitOfWork>();
        var userLinks = Substitute.For<ITelegramUserLinkRepository>();
        var inboundUpdates = Substitute.For<ITelegramInboundUpdateRepository>();
        var signal = Substitute.For<ITelegramUpdateSignal>();
        unitOfWork.UserLinksRepository.Returns(userLinks);
        unitOfWork.InboundUpdatesRepository.Returns(inboundUpdates);

        return new Fixture(
            unitOfWork,
            userLinks,
            inboundUpdates,
            signal,
            new FixedTimeProvider(Now),
            CancellationToken.None);
    }

    private sealed record Fixture(
        ITelegramUnitOfWork UnitOfWork,
        ITelegramUserLinkRepository UserLinks,
        ITelegramInboundUpdateRepository InboundUpdates,
        ITelegramUpdateSignal Signal,
        TimeProvider TimeProvider,
        CancellationToken Token);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
