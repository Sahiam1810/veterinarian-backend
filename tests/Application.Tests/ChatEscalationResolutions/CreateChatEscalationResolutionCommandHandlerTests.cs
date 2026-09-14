using Application.ChatEscalationResolutions.UseCase;
using Application.Common.Abstractions;
using Application.Notifications.Abstraction;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;

namespace Application.Tests.ChatEscalationResolutions;

public sealed class CreateChatEscalationResolutionCommandHandlerTests
{
    private static readonly Guid ConversationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid EscalationStatusId = Guid.Parse("85000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Resolving_an_escalation_broadcasts_the_resolved_event()
    {
        var fixture = CreateFixture();
        var escalation = ChatEscalationEntity.Create(ConversationId, EscalationStatusId, false, "asesor");
        var resolvedBy = Guid.NewGuid();
        fixture.Uow.ChatEscalationsRepository.GetByIdAsync(escalation.Id, default).Returns(escalation);

        var resolution = await fixture.Handler.Handle(
            new CreateChatEscalationResolutionCommand(escalation.Id, resolvedBy, "listo", null),
            default);

        await fixture.ChatRealtimeNotifier.Received(1).NotifyEscalationResolvedAsync(
            Arg.Is<ChatEscalationResolvedPayload>(payload =>
                payload.EscalationId == escalation.Id &&
                payload.ConversationId == ConversationId &&
                payload.ResolvedBy == resolvedBy.ToString() &&
                payload.ResolutionNotes == "listo" &&
                payload.ResolvedAt == resolution.ResolvedAt),
            default);
    }

    [Fact]
    public async Task Broadcast_failure_does_not_prevent_resolution_creation()
    {
        var fixture = CreateFixture();
        var escalation = ChatEscalationEntity.Create(ConversationId, EscalationStatusId, false, "asesor");
        fixture.Uow.ChatEscalationsRepository.GetByIdAsync(escalation.Id, default).Returns(escalation);
        fixture.ChatRealtimeNotifier
            .NotifyEscalationResolvedAsync(Arg.Any<ChatEscalationResolvedPayload>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("hub unavailable"));

        var resolution = await fixture.Handler.Handle(
            new CreateChatEscalationResolutionCommand(escalation.Id, null, null, null),
            default);

        Assert.NotNull(resolution);
    }

    private static Fixture CreateFixture()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var chatRealtimeNotifier = Substitute.For<IChatRealtimeNotifier>();
        var logger = Substitute.For<ILogger<CreateChatEscalationResolutionCommandHandler>>();

        return new Fixture(
            new CreateChatEscalationResolutionCommandHandler(uow, chatRealtimeNotifier, logger),
            uow,
            chatRealtimeNotifier);
    }

    private sealed record Fixture(
        CreateChatEscalationResolutionCommandHandler Handler,
        IUnitOfWork Uow,
        IChatRealtimeNotifier ChatRealtimeNotifier);
}
