using Application.Agent.Abstractions;
using Application.ChatMessages.Events;
using Application.ChatMessages.Notifications;
using Application.Common.Abstractions;
using Application.Notifications.Abstraction;
using Domain.MessageTypes.Entities;
using Domain.SenderTypes.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using ChatMessageEntity = Domain.ChatMessages.Entities.ChatMessage;

namespace Application.Tests.ChatMessages;

public sealed class NotifyReceptionOfNewChatMessageHandlerTests
{
    private static readonly Guid ConversationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ParticipantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid SenderTypeId = Guid.Parse("82000000-0000-0000-0000-000000000001");
    private static readonly Guid MessageTypeId = Guid.Parse("83000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Message_in_a_conversation_without_active_escalation_is_not_broadcast()
    {
        var fixture = CreateFixture();
        fixture.EscalationReader.HasActiveAsync(ConversationId, default).Returns(false);
        var message = ChatMessageEntity.Create(
            ConversationId, SenderTypeId, MessageTypeId, ParticipantId, "hola");

        await fixture.Handler.Handle(new ChatMessageCreatedNotification(message), default);

        await fixture.ChatRealtimeNotifier.DidNotReceive().NotifyMessageReceivedAsync(
            Arg.Any<ChatMessageReceivedPayload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Message_in_a_conversation_with_active_escalation_is_broadcast_with_resolved_names()
    {
        var fixture = CreateFixture();
        fixture.EscalationReader.HasActiveAsync(ConversationId, default).Returns(true);
        fixture.Uow.SenderTypesRepository.GetByIdAsync(SenderTypeId, default)
            .Returns(new SenderTypeEntity("Cliente"));
        fixture.Uow.MessageTypesRepository.GetByIdAsync(MessageTypeId, default)
            .Returns(new MessageTypeEntity("Texto"));
        var message = ChatMessageEntity.Create(
            ConversationId, SenderTypeId, MessageTypeId, ParticipantId, "hola");

        await fixture.Handler.Handle(new ChatMessageCreatedNotification(message), default);

        await fixture.ChatRealtimeNotifier.Received(1).NotifyMessageReceivedAsync(
            Arg.Is<ChatMessageReceivedPayload>(payload =>
                payload.MessageId == message.Id &&
                payload.ConversationId == ConversationId &&
                payload.SenderType == "Cliente" &&
                payload.MessageType == "Texto" &&
                payload.Content == "hola"),
            default);
    }

    [Fact]
    public async Task Broadcast_failure_does_not_propagate()
    {
        var fixture = CreateFixture();
        fixture.EscalationReader.HasActiveAsync(ConversationId, default).Returns(true);
        fixture.Uow.SenderTypesRepository.GetByIdAsync(SenderTypeId, default)
            .Returns(new SenderTypeEntity("Cliente"));
        fixture.Uow.MessageTypesRepository.GetByIdAsync(MessageTypeId, default)
            .Returns(new MessageTypeEntity("Texto"));
        fixture.ChatRealtimeNotifier
            .NotifyMessageReceivedAsync(Arg.Any<ChatMessageReceivedPayload>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("hub unavailable"));
        var message = ChatMessageEntity.Create(
            ConversationId, SenderTypeId, MessageTypeId, ParticipantId, "hola");

        var exception = await Record.ExceptionAsync(() =>
            fixture.Handler.Handle(new ChatMessageCreatedNotification(message), default));

        Assert.Null(exception);
    }

    private static Fixture CreateFixture()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var escalationReader = Substitute.For<IActiveConversationEscalationReader>();
        var chatRealtimeNotifier = Substitute.For<IChatRealtimeNotifier>();
        var logger = Substitute.For<ILogger<NotifyReceptionOfNewChatMessageHandler>>();

        return new Fixture(
            new NotifyReceptionOfNewChatMessageHandler(uow, escalationReader, chatRealtimeNotifier, logger),
            uow,
            escalationReader,
            chatRealtimeNotifier);
    }

    private sealed record Fixture(
        NotifyReceptionOfNewChatMessageHandler Handler,
        IUnitOfWork Uow,
        IActiveConversationEscalationReader EscalationReader,
        IChatRealtimeNotifier ChatRealtimeNotifier);
}
