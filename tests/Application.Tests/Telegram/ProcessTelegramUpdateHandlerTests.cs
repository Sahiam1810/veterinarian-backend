using Application.Agent.Abstractions;
using Application.Agent.Errors;
using Application.Agent.Messages;
using Application.Telegram.Abstractions;
using Application.Telegram.Identity;
using Application.Telegram.Models;
using Application.Telegram.Processing;
using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
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
    public async Task Guest_mode_disabled_returns_control_message_without_linking_command()
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
            Arg.Is<string>(text =>
                text.Contains("invitado", StringComparison.OrdinalIgnoreCase) &&
                !text.Contains("/vincular", StringComparison.OrdinalIgnoreCase)),
            default);
        await fixture.Dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<AgentMessageDispatchRequest>(),
            Arg.Any<AgentConversationContext>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_explains_public_access_without_linking_commands()
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
            Arg.Is<string>(text =>
                text.Contains("generales", StringComparison.OrdinalIgnoreCase) &&
                !text.Contains("/vincular", StringComparison.OrdinalIgnoreCase)),
            default);
        await fixture.Dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<AgentMessageDispatchRequest>(),
            Arg.Any<AgentConversationContext>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task General_guest_response_is_delivered_without_automatic_linking_suffix()
    {
        var fixture = CreateFixture();
        var guestId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var update = ProcessingUpdate(50, "¿Cómo cuido a un cachorro?");
        fixture.Settings.GuestModeEnabled.Returns(true);
        fixture.Updates.GetByIdAsync(50, default).Returns(update);
        fixture.UserLinks.GetByTelegramUserIdAsync(1001, default)
            .Returns((TelegramUserLink?)null);
        fixture.Identity.GetGuest(1001)
            .Returns(new AgentDelegatedIdentity(guestId, "TelegramGuest", "guest-token"));
        fixture.Dispatcher.DispatchAsync(
                Arg.Any<AgentMessageDispatchRequest>(),
                Arg.Any<AgentConversationContext>(),
                "guest-token",
                default)
            .Returns(Result("Cuidados generales"));

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(50), default);

        await fixture.Dispatcher.Received(1).DispatchAsync(
            Arg.Is<AgentMessageDispatchRequest>(request =>
                request.PersonId == guestId && request.Role == "TelegramGuest"),
            Arg.Is<AgentConversationContext>(context =>
                context.Channel == "telegram" && !context.IsEscalated),
            "guest-token",
            default);
        await fixture.Context.DidNotReceive().ResolveAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid?>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await fixture.Bot.Received(1).SendTextAsync(
            1001,
            "Cuidados generales",
            default);
    }

    [Fact]
    public async Task Guest_start_explains_both_modes_without_calling_the_agent()
    {
        var fixture = CreateFixture();
        var update = ProcessingUpdate(51, "/start");
        fixture.Settings.GuestModeEnabled.Returns(true);
        fixture.Updates.GetByIdAsync(51, default).Returns(update);
        fixture.UserLinks.GetByTelegramUserIdAsync(1001, default)
            .Returns((TelegramUserLink?)null);

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(51), default);

        await fixture.Dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<AgentMessageDispatchRequest>(),
            Arg.Any<AgentConversationContext>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await fixture.Bot.Received(1).SendTextAsync(
            1001,
            Arg.Is<string>(text =>
                text.Contains("invitado", StringComparison.OrdinalIgnoreCase) &&
                text.Contains("solo", StringComparison.OrdinalIgnoreCase) &&
                !text.Contains("/vincular", StringComparison.OrdinalIgnoreCase) &&
                !text.Contains("/registrar", StringComparison.OrdinalIgnoreCase)),
            default);
    }

    [Fact]
    public async Task Linked_user_reuses_open_conversation_and_sends_agent_response()
    {
        var fixture = CreateFixture();
        var update = ProcessingUpdate(43, "¿Qué vacunas necesita?");
        var userLink = TelegramUserLink.Create(PersonId, 1001, 1001, Now.UtcDateTime);
        fixture.Updates.GetByIdAsync(43, default).Returns(update);
        fixture.UserLinks.GetByTelegramUserIdAsync(1001, default).Returns(userLink);
        fixture.Access.HasValidAccessAsync(1001, Now.UtcDateTime, default).Returns(true);
        fixture.ConversationLinks.GetBindingAsync(userLink.Id, default)
            .Returns(new TelegramConversationBinding(ConversationId, false));
        fixture.Context.ResolveAsync(PersonId, ConversationId, "telegram-update-43-verified", default)
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
                request.IdempotencyKey == "telegram-update-43-verified"),
            Arg.Is<AgentConversationContext>(context => context.Channel == "telegram"),
            "delegated-token",
            default);
        await fixture.Bot.Received(1).SendTextAsync(1001, "Respuesta veterinaria", default);
    }

    [Fact]
    public async Task Legacy_linking_command_is_explained_by_identity_service()
    {
        var fixture = CreateFixture();
        var update = ProcessingUpdate(48, "/vincular");
        fixture.Updates.GetByIdAsync(48, default).Returns(update);
        fixture.Access.HandleActiveFlowAsync(update, default)
            .Returns(new TelegramIdentityAccessOutcome(true, "La verificación inicia automáticamente."));

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(48), default);

        Assert.Equal(TelegramInboundUpdateStatus.Completed, update.Status);
        await fixture.Bot.Received(1).SendTextAsync(
            1001,
            "La verificación inicia automáticamente.",
            default);
        await fixture.Dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<AgentMessageDispatchRequest>(),
            Arg.Any<AgentConversationContext>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Legacy_registration_command_is_explained_before_agent_call()
    {
        var fixture = CreateFixture();
        var update = ProcessingUpdate(53, "/registrar");
        fixture.Updates.GetByIdAsync(53, default).Returns(update);
        fixture.Access.HandleActiveFlowAsync(update, default)
            .Returns(new TelegramIdentityAccessOutcome(true, "El registro inicia automáticamente."));

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(53), default);

        await fixture.Bot.Received(1).SendTextAsync(
            1001,
            "El registro inicia automáticamente.",
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

    [Fact]
    public async Task Agent_failure_is_logged_without_request_content()
    {
        var fixture = CreateFixture();
        var update = ProcessingUpdate(52, "contenido sensible");
        var userLink = TelegramUserLink.Create(PersonId, 1001, 1001, Now.UtcDateTime);
        fixture.Updates.GetByIdAsync(52, default).Returns(update);
        fixture.UserLinks.GetByTelegramUserIdAsync(1001, default).Returns(userLink);
        fixture.Access.HasValidAccessAsync(1001, Now.UtcDateTime, default).Returns(true);
        fixture.ConversationLinks.GetBindingAsync(userLink.Id, default)
            .Returns(new TelegramConversationBinding(ConversationId, false));
        fixture.Context.ResolveAsync(PersonId, ConversationId, "telegram-update-52-verified", default)
            .Returns(new AgentConversationContext(ConversationId, "web", false));
        fixture.Identity.GetAsync(PersonId, default)
            .Returns(new AgentDelegatedIdentity(PersonId, "Cliente", "delegated-token"));
        fixture.Dispatcher.DispatchAsync(
                Arg.Any<AgentMessageDispatchRequest>(),
                Arg.Any<AgentConversationContext>(),
                "delegated-token",
                default)
            .Returns<Task<AgentMessageResult>>(_ => throw new AgentUnavailableException(
                new HttpRequestException("Connection refused")));

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(52), default);

        Assert.IsType<AgentUnavailableException>(fixture.Logger.Exception);
        Assert.DoesNotContain("contenido sensible", fixture.Logger.Message, StringComparison.Ordinal);
        Assert.Contains("agent_request_failed", fixture.Logger.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Private_guest_result_starts_identity_verification()
    {
        var fixture = CreateFixture();
        var guestId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var update = ProcessingUpdate(60, "quiero ver mis mascotas");
        fixture.Settings.GuestModeEnabled.Returns(true);
        fixture.Updates.GetByIdAsync(60, default).Returns(update);
        fixture.Identity.GetGuest(1001)
            .Returns(new AgentDelegatedIdentity(guestId, "TelegramGuest", "guest-token"));
        fixture.Dispatcher.DispatchAsync(
                Arg.Any<AgentMessageDispatchRequest>(),
                Arg.Any<AgentConversationContext>(),
                "guest-token",
                default)
            .Returns(Result(
                "Necesito verificar tu identidad.",
                AgentAccessRequirement.IdentityVerification));
        fixture.Access.BeginPrivateAccessAsync(update, default)
            .Returns(new TelegramIdentityAccessOutcome(true, "Escribe tu cédula."));

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(60), default);

        await fixture.Access.Received(1).BeginPrivateAccessAsync(update, default);
        await fixture.Bot.Received(1).SendTextAsync(1001, "Escribe tu cédula.", default);
    }

    [Fact]
    public async Task Valid_otp_outcome_resumes_original_private_message_once()
    {
        var fixture = CreateFixture();
        var otpUpdate = ProcessingUpdate(61, "123456");
        var userLink = TelegramUserLink.Create(PersonId, 1001, 1001, Now.UtcDateTime);
        fixture.Updates.GetByIdAsync(61, default).Returns(otpUpdate);
        fixture.Access.HandleActiveFlowAsync(otpUpdate, default)
            .Returns(new TelegramIdentityAccessOutcome(
                true,
                "Identidad verificada.",
                PersonId,
                60,
                "quiero ver mis mascotas"));
        fixture.UserLinks.GetByTelegramUserIdAsync(1001, default).Returns(userLink);
        fixture.ConversationLinks.GetBindingAsync(userLink.Id, default)
            .Returns(new TelegramConversationBinding(ConversationId, false));
        fixture.Context.ResolveAsync(PersonId, ConversationId, "telegram-update-60-verified", default)
            .Returns(new AgentConversationContext(ConversationId, "web", false));
        fixture.Identity.GetAsync(PersonId, default)
            .Returns(new AgentDelegatedIdentity(PersonId, "Cliente", "delegated-token"));
        fixture.Dispatcher.DispatchAsync(
                Arg.Any<AgentMessageDispatchRequest>(),
                Arg.Any<AgentConversationContext>(),
                "delegated-token",
                default)
            .Returns(Result("Tienes una mascota registrada"));

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(61), default);

        await fixture.Dispatcher.Received(1).DispatchAsync(
            Arg.Is<AgentMessageDispatchRequest>(request =>
                request.Message == "quiero ver mis mascotas" &&
                request.IdempotencyKey == "telegram-update-60-verified"),
            Arg.Any<AgentConversationContext>(),
            "delegated-token",
            default);
        await fixture.Bot.Received(1).SendTextAsync(
            1001,
            "Tienes una mascota registrada",
            default);
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
        var access = Substitute.For<ITelegramIdentityAccessService>();
        var logger = new RecordingLogger<ProcessTelegramUpdateHandler>();
        access.HandleActiveFlowAsync(
                Arg.Any<TelegramInboundUpdate>(),
                Arg.Any<CancellationToken>())
            .Returns(new TelegramIdentityAccessOutcome(false, null));

        return new Fixture(
            new ProcessTelegramUpdateHandler(
                unitOfWork,
                context,
                dispatcher,
                identity,
                bot,
                sender,
                access,
                settings,
                new FixedTimeProvider(currentTime ?? Now),
                logger),
            updates,
            userLinks,
            conversationLinks,
            context,
            dispatcher,
            identity,
            bot,
            sender,
            access,
            settings,
            logger);
    }

    private static TelegramInboundUpdate ProcessingUpdate(long id, string text)
    {
        var update = TelegramInboundUpdate.Create(id, 1001, 1001, 7, "private", text, Now.UtcDateTime);
        update.Claim(Now.UtcDateTime);
        return update;
    }

    private static AgentMessageResult Result(
        string message,
        AgentAccessRequirement accessRequirement = AgentAccessRequirement.None) =>
        new(message, ConversationId, Guid.NewGuid(), "ai_generated", "openai", "gpt", null, null,
            new AgentRagResult("used", "contextual", 0.9, 1, 1, true, false),
            accessRequirement);

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
        ITelegramIdentityAccessService Access,
        ITelegramRuntimeSettings Settings,
        RecordingLogger<ProcessTelegramUpdateHandler> Logger);

    private sealed class RecordingLogger<T> : ILogger<T>
    {
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
            Exception = exception;
            Message = formatter(state, exception);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
