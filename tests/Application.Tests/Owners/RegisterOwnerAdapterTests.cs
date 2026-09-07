using Application.Owners.Abstractions;
using Application.Owners.Adapters;
using Application.Owners.Enums;
using Application.Owners.UseCases;
using MediatR;
using NSubstitute;
using Xunit;

namespace Application.Tests.Owners;

public sealed class RegisterOwnerAdapterTests
{
    private readonly ISender sender = Substitute.For<ISender>();

    public RegisterOwnerAdapterTests()
    {
        sender.Send(Arg.Any<RegisterOwnerCommand>(), Arg.Any<CancellationToken>())
            .Returns(new RegisterOwnerResult(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task Staff_adapter_sends_staff_channel()
    {
        var adapter = new RegisterOwnerFromStaff(sender);

        await adapter.RegisterAsync(
            new RegisterOwnerFromStaffRequest("Ana", "ana@huellitas.test", "123", "3001234567"),
            CancellationToken.None);

        await sender.Received(1).Send(
            Arg.Is<RegisterOwnerCommand>(c => c.Channel == RegisterOwnerChannel.Staff),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Bot_adapter_sends_bot_channel_with_proof()
    {
        var adapter = new RegisterOwnerFromBot(sender);
        var sessionId = Guid.NewGuid();

        await adapter.RegisterAsync(
            new RegisterOwnerFromBotRequest(
                "Ana", "ana@huellitas.test", "123", "3001234567", sessionId, "proof"),
            CancellationToken.None);

        await sender.Received(1).Send(
            Arg.Is<RegisterOwnerCommand>(c =>
                c.Channel == RegisterOwnerChannel.Bot &&
                c.ContactProofSessionId == sessionId &&
                c.ContactProof == "proof"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Telegram_adapter_sends_telegram_channel_without_requiring_proof()
    {
        var adapter = new RegisterOwnerFromTelegram(sender);

        await adapter.RegisterAsync(
            new RegisterOwnerFromTelegramRequest(
                "Ana", "ana@huellitas.test", "123", "3001234567", TelegramUserId: 99),
            CancellationToken.None);

        await sender.Received(1).Send(
            Arg.Is<RegisterOwnerCommand>(c =>
                c.Channel == RegisterOwnerChannel.Telegram &&
                c.ContactProofSessionId == null &&
                c.ContactProof == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cleanup_stub_does_not_sweep_yet()
    {
        var cleanup = new RegisterOwnerCleanupStub();

        var removed = await cleanup.SweepExpiredSessionsAsync(CancellationToken.None);

        Assert.Equal(0, removed);
    }
}
