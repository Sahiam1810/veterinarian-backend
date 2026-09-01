using Application.Agent.Abstractions;
using Application.Agent.Messages;
using Application.Telegram.Abstractions;
using Application.Telegram.Models;
using Application.Telegram.Linking;
using Application.Telegram.Processing;
using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using MediatR;
using NSubstitute;
using Xunit;

namespace Application.Tests.Telegram;

public sealed class ProcessTelegramUpdateHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 31, 18, 0, 0, TimeSpan.Zero);
    private static readonly Guid PersonId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ConversationId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Unlinked_user_receives_control_message_without_agent_call()
    {
        var fixture = CreateFixture();
        var update = ProcessingUpdate(42, "hola");
        fixture.Updates.GetByIdAsync(42, default).Returns(update);
        fixture.UserLinks.GetByTelegramUserIdAsync(1001, default)
            .Returns((TelegramUserLink?)null);

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(42), default);

        Assert.Equal(TelegramInboundUpdateStatus.Completed, update.Status);
        await fixture.Bot.Received(1).SendTextAsync(
            1001,
            Arg.Is<string>(text => text.Contains("/vincular", StringComparison.OrdinalIgnoreCase)),
            default);
        await fixture.Dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<AgentMessageDispatchRequest>(),
            Arg.Any<AgentConversationContext>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_without_link_code_invites_the_user_to_link_before_conversing()
    {
        var fixture = CreateFixture();
        var update = ProcessingUpdate(49, "/start");
        fixture.Updates.GetByIdAsync(49, default).Returns(update);
        fixture.UserLinks.GetByTelegramUserIdAsync(1001, default)
            .Returns((TelegramUserLink?)null);

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(49), default);

        Assert.Equal(TelegramInboundUpdateStatus.Completed, update.Status);
        await fixture.Bot.Received(1).SendTextAsync(
            1001,
            Arg.Is<string>(text => text.Contains("/vincular", StringComparison.OrdinalIgnoreCase)),
            default);
        await fixture.Dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<AgentMessageDispatchRequest>(),
            Arg.Any<AgentConversationContext>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Linked_user_reuses_open_conversation_and_sends_agent_response()
    {
        var fixture = CreateFixture();
        var update = ProcessingUpdate(43, "¿Qué vacunas necesita?");
        var userLink = TelegramUserLink.Create(PersonId, 1001, 1001, Now.UtcDateTime);
        fixture.Updates.GetByIdAsync(43, default).Returns(update);
        fixture.UserLinks.GetByTelegramUserIdAsync(1001, default).Returns(userLink);
        fixture.ConversationLinks.GetBindingAsync(userLink.Id, default)
            .Returns(new TelegramConversationBinding(ConversationId, false));
        fixture.Context.ResolveAsync(PersonId, ConversationId, "telegram-update-43", default)
            .Returns(new AgentConversationContext(ConversationId, "web", false));
        fixture.Identity.GetAsync(PersonId, default)
            .Returns(new AgentDelegatedIdentity(PersonId, "Cliente", "delegated-token"));
        fixture.Dispatcher.DispatchAsync(
                Arg.Any<AgentMessageDispatchRequest>(),
                Arg.Any<AgentConversationContext>(),
                "delegated-token",
                default)
            .Returns(Result("Respuesta veterinaria"));

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(43), default);

        Assert.Equal(TelegramInboundUpdateStatus.Completed, update.Status);
        await fixture.Dispatcher.Received(1).DispatchAsync(
            Arg.Is<AgentMessageDispatchRequest>(request =>
                request.IdempotencyKey == "telegram-update-43"),
            Arg.Is<AgentConversationContext>(context => context.Channel == "telegram"),
            "delegated-token",
            default);
        await fixture.Bot.Received(1).SendTextAsync(1001, "Respuesta veterinaria", default);
    }

    [Fact]
    public async Task Linking_message_is_delivered_without_calling_the_agent()
    {
        var fixture = CreateFixture();
        var update = ProcessingUpdate(48, "/vincular");
        fixture.Updates.GetByIdAsync(48, default).Returns(update);
        fixture.Linking.HandleAsync(update, default)
            .Returns(new TelegramLinkingOutcome(true, "Escribe tu correo registrado."));

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(48), default);

        Assert.Equal(TelegramInboundUpdateStatus.Completed, update.Status);
        await fixture.Bot.Received(1).SendTextAsync(
            1001,
            "Escribe tu correo registrado.",
            default);
        await fixture.Dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<AgentMessageDispatchRequest>(),
            Arg.Any<AgentConversationContext>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Retry_resumes_prepared_response_without_reprocessing_the_command()
    {
        var retryAt = Now.AddSeconds(1);
        var fixture = CreateFixture(retryAt);
        var update = ProcessingUpdate(44, "/start one-use-code");
        update.PrepareResponse("Tu cuenta quedó vinculada.", Now.UtcDateTime);
        update.ScheduleRetry(retryAt.UtcDateTime, "telegram_delivery_failed", 3, Now.UtcDateTime);
        update.Claim(retryAt.UtcDateTime);
        fixture.Updates.GetByIdAsync(44, default).Returns(update);

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(44), default);

        Assert.Equal(TelegramInboundUpdateStatus.Completed, update.Status);
        await fixture.Bot.Received(1).SendTextAsync(1001, "Tu cuenta quedó vinculada.", default);
        await fixture.Sender.DidNotReceive().Send(Arg.Any<IRequest>(), Arg.Any<CancellationToken>());
        await fixture.Dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<AgentMessageDispatchRequest>(),
            Arg.Any<AgentConversationContext>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    private static Fixture CreateFixture(DateTimeOffset? currentTime = null)
    {
        var unitOfWork = Substitute.For<ITelegramUnitOfWork>();
        var updates = Substitute.For<ITelegramInboundUpdateRepository>();
        var userLinks = Substitute.For<ITelegramUserLinkRepository>();
        var conversationLinks = Substitute.For<ITelegramConversationLinkRepository>();
        unitOfWork.InboundUpdatesRepository.Returns(updates);
        unitOfWork.UserLinksRepository.Returns(userLinks);
        unitOfWork.ConversationLinksRepository.Returns(conversationLinks);
        var context = Substitute.For<IConversationContextProvider>();
        var dispatcher = Substitute.For<IAgentMessageDispatcher>();
        var identity = Substitute.For<IAgentDelegatedIdentityProvider>();
        var bot = Substitute.For<ITelegramBotClient>();
        var sender = Substitute.For<ISender>();
        var settings = Substitute.For<ITelegramRuntimeSettings>();
        settings.MaxProcessingAttempts.Returns(3);
        var linking = Substitute.For<ITelegramChatLinkingService>();
        linking.HandleAsync(Arg.Any<TelegramInboundUpdate>(), Arg.Any<CancellationToken>())
            .Returns(new TelegramLinkingOutcome(false, null));

        return new Fixture(
            new ProcessTelegramUpdateHandler(
                unitOfWork,
                context,
                dispatcher,
                identity,
                bot,
                sender,
                linking,
                settings,
                new FixedTimeProvider(currentTime ?? Now)),
            updates,
            userLinks,
            conversationLinks,
            context,
            dispatcher,
            identity,
            bot,
            sender,
            linking);
    }

    private static TelegramInboundUpdate ProcessingUpdate(long id, string text)
    {
        var update = TelegramInboundUpdate.Create(id, 1001, 1001, 7, "private", text, Now.UtcDateTime);
        update.Claim(Now.UtcDateTime);
        return update;
    }

    private static AgentMessageResult Result(string message) =>
        new(message, ConversationId, Guid.NewGuid(), "ai_generated", "openai", "gpt", null, null,
            new AgentRagResult("used", "contextual", 0.9, 1, 1, true, false));

    private sealed record Fixture(
        ProcessTelegramUpdateHandler Handler,
        ITelegramInboundUpdateRepository Updates,
        ITelegramUserLinkRepository UserLinks,
        ITelegramConversationLinkRepository ConversationLinks,
        IConversationContextProvider Context,
        IAgentMessageDispatcher Dispatcher,
        IAgentDelegatedIdentityProvider Identity,
        ITelegramBotClient Bot,
        ISender Sender,
        ITelegramChatLinkingService Linking);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
