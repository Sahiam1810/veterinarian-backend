using Application.ChatMessages.Events;
using Application.Telegram.Abstractions;
using Application.Telegram.Notifications;
using Domain.Telegram.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using ChatMessageEntity = Domain.ChatMessages.Entities.ChatMessage;

namespace Application.Tests.Telegram;

public sealed class ForwardHumanChatMessageToTelegramHandlerTests
{
    private static readonly Guid ConversationId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ParticipantId =
        Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid HumanAgentSenderTypeId =
        Guid.Parse("82000000-0000-0000-0000-000000000003");
    private static readonly Guid ClientSenderTypeId =
        Guid.Parse("82000000-0000-0000-0000-000000000001");
    private static readonly Guid AiAgentSenderTypeId =
        Guid.Parse("82000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task Human_agent_message_is_forwarded_to_the_linked_telegram_chat()
    {
        var fixture = CreateFixture();
        var message = HumanAgentMessage("Ya puedes traer a tu mascota mañana a las 9am.");
        var userLink = TelegramUserLink.Create(Guid.NewGuid(), 1001, 1001, DateTime.UtcNow);
        userLink.BindConversation(ConversationId);
        fixture.UserLinks.GetByConversationIdAsync(ConversationId, default).Returns(userLink);

        await fixture.Handler.Handle(new ChatMessageCreatedNotification(message), default);

        await fixture.Bot.Received(1).SendTextAsync(
            1001,
            "Ya puedes traer a tu mascota mañana a las 9am.",
            default);
    }

    [Theory]
    [MemberData(nameof(NonHumanAgentSenderTypeIds))]
    public async Task Message_from_a_non_human_agent_sender_is_never_forwarded(Guid senderTypeId)
    {
        var fixture = CreateFixture();
        var message = ChatMessageEntity.Create(
            ConversationId, senderTypeId, ParticipantId, "hola");

        await fixture.Handler.Handle(new ChatMessageCreatedNotification(message), default);

        await fixture.Bot.DidNotReceive().SendTextAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await fixture.UserLinks.DidNotReceive().GetByConversationIdAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    public static TheoryData<Guid> NonHumanAgentSenderTypeIds() =>
        new() { ClientSenderTypeId, AiAgentSenderTypeId };

    [Fact]
    public async Task Missing_user_link_logs_an_error_without_throwing()
    {
        var fixture = CreateFixture();
        var message = HumanAgentMessage("Respuesta del asesor");
        fixture.UserLinks.GetByConversationIdAsync(ConversationId, default)
            .Returns((TelegramUserLink?)null);

        await fixture.Handler.Handle(new ChatMessageCreatedNotification(message), default);

        await fixture.Bot.DidNotReceive().SendTextAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Equal(LogLevel.Error, fixture.Logger.Level);
        Assert.Contains(message.ChatConversationId.ToString(), fixture.Logger.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Delivery_failure_is_logged_without_propagating_the_exception()
    {
        var fixture = CreateFixture();
        var message = HumanAgentMessage("Respuesta del asesor");
        var userLink = TelegramUserLink.Create(Guid.NewGuid(), 1001, 1001, DateTime.UtcNow);
        userLink.BindConversation(ConversationId);
        fixture.UserLinks.GetByConversationIdAsync(ConversationId, default).Returns(userLink);
        fixture.Bot.SendTextAsync(1001, Arg.Any<string>(), default)
            .Returns<Task<long>>(_ => throw new InvalidOperationException("Telegram API unavailable"));

        var exception = await Record.ExceptionAsync(() =>
            fixture.Handler.Handle(new ChatMessageCreatedNotification(message), default));

        Assert.Null(exception);
        Assert.Equal(LogLevel.Error, fixture.Logger.Level);
        Assert.IsType<InvalidOperationException>(fixture.Logger.Exception);
    }

    private static ChatMessageEntity HumanAgentMessage(string content) =>
        ChatMessageEntity.Create(
            ConversationId, HumanAgentSenderTypeId, ParticipantId, content);

    private static Fixture CreateFixture()
    {
        var userLinks = Substitute.For<ITelegramUserLinkRepository>();
        var bot = Substitute.For<ITelegramBotClient>();
        var settings = Substitute.For<ITelegramRuntimeSettings>();
        settings.HumanAgentSenderTypeId.Returns(HumanAgentSenderTypeId);
        var logger = new RecordingLogger<ForwardHumanChatMessageToTelegramHandler>();

        return new Fixture(
            new ForwardHumanChatMessageToTelegramHandler(
                userLinks, bot, settings, logger),
            userLinks,
            bot,
            logger);
    }

    private sealed record Fixture(
        ForwardHumanChatMessageToTelegramHandler Handler,
        ITelegramUserLinkRepository UserLinks,
        ITelegramBotClient Bot,
        RecordingLogger<ForwardHumanChatMessageToTelegramHandler> Logger);

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public LogLevel? Level { get; private set; }

        public Exception? Exception { get; private set; }

        public string Message { get; private set; } = string.Empty;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Level = logLevel;
            Exception = exception;
            Message = formatter(state, exception);
        }
    }
}
