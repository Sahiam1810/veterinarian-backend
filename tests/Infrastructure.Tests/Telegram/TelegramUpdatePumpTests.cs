using Application.Telegram.Abstractions;
using Application.Telegram.Processing;
using Domain.Telegram.Entities;
using Infrastructure.Telegram.Workers;
using MediatR;
using NSubstitute;
using Xunit;

namespace Infrastructure.Tests.Telegram;

public sealed class TelegramUpdatePumpTests
{
    [Fact]
    public async Task Run_once_claims_one_update_and_dispatches_processing()
    {
        var unitOfWork = Substitute.For<ITelegramUnitOfWork>();
        var repository = Substitute.For<ITelegramInboundUpdateRepository>();
        var sender = Substitute.For<ISender>();
        var settings = Substitute.For<ITelegramRuntimeSettings>();
        settings.ProcessingLease.Returns(TimeSpan.FromMinutes(5));
        unitOfWork.InboundUpdatesRepository.Returns(repository);
        var now = new DateTimeOffset(2026, 8, 31, 18, 0, 0, TimeSpan.Zero);
        var update = TelegramInboundUpdate.Create(77, 1001, 1001, 9, "private", "hola", now.UtcDateTime);
        update.Claim(now.UtcDateTime);
        repository.ClaimNextAsync(
                now.UtcDateTime,
                now.UtcDateTime.AddMinutes(-5),
                default)
            .Returns(update);

        var processed = await new TelegramUpdatePump(
            unitOfWork,
            sender,
            settings,
            new FixedTimeProvider(now)).RunOnceAsync(default);

        Assert.True(processed);
        await sender.Received(1).Send(new ProcessTelegramUpdateCommand(77), default);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
