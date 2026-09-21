using Application.Agent.Abstractions;
using Application.Agent.Errors;
using Application.Agent.Messages;
using Application.ChatEscalations.UseCase;
using Application.ChatMessages.UseCase;
using Application.ChatParticipants.UseCase;
using Application.Telegram.Abstractions;
using Application.Telegram.Models;
using Application.Telegram.Processing;
using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;
using ChatParticipantEntity = Domain.ChatParticipants.Entities.ChatParticipant;

namespace Application.Tests.Telegram;

public sealed class ProcessTelegramUpdateHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 31, 18, 0, 0, TimeSpan.Zero);
    private static readonly Guid PersonId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ConversationId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid PendingEscalationStatusId =
        Guid.Parse("85000000-0000-0000-0000-000000000001");
    private static readonly Guid ClientParticipantTypeId =
        Guid.Parse("82000000-0000-0000-0000-000000000001");
    private static readonly Guid TextMessageTypeId =
        Guid.Parse("83000000-0000-0000-0000-000000000001");

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
            Arg.Is<string>(text => !text.Contains("/vincular", StringComparison.OrdinalIgnoreCase)),
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
    public async Task Start_with_payload_returns_the_same_guest_welcome_message()
    {
        var fixture = CreateFixture();
        var update = ProcessingUpdate(49, "/start abc123");
        fixture.Updates.GetByIdAsync(49, default).Returns(update);
        fixture.UserLinks.GetByTelegramUserIdAsync(1001, default)
            .Returns((TelegramUserLink?)null);

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(49), default);

        Assert.Equal(TelegramInboundUpdateStatus.Completed, update.Status);
        await fixture.Bot.Received(1).SendTextAsync(
            1001,
            Arg.Is<string>(text =>
                text.Contains("generales", StringComparison.OrdinalIgnoreCase)),
            default);
        await fixture.Sender.DidNotReceive().Send(
            Arg.Any<IRequest>(),
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
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await fixture.Bot.Received(1).SendTextAsync(
            1001,
            "Cuidados generales",
            default);
    }

    [Fact]
    public async Task Guest_response_is_delivered_as_is_regardless_of_access_requirement()
    {
        // Sin verificación de identidad: la respuesta del agente se entrega tal
        // cual, sin que el backend interprete ni actúe sobre AccessRequirement.
        var fixture = CreateFixture();
        var guestId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var update = ProcessingUpdate(60, "quiero agendar una cita");
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
                "Para agendar necesito tu nombre, cédula, correo y teléfono.",
                AgentAccessRequirement.IdentityVerification,
                "Quiero agendar una cita"));

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(60), default);

        await fixture.Bot.Received(1).SendTextAsync(
            1001,
            "Para agendar necesito tu nombre, cédula, correo y teléfono.",
            default);
        await fixture.Identity.DidNotReceive().GetAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Guest_link_with_resume_message_continues_as_authenticated_client()
    {
        var fixture = CreateFixture();
        var guestId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var update = ProcessingUpdate(61, "Ana Perez\n123456789\nana@example.test\n3001234567");
        var userLink = TelegramUserLink.Create(PersonId, 1001, 1001, Now.UtcDateTime);
        fixture.Settings.GuestModeEnabled.Returns(true);
        fixture.Updates.GetByIdAsync(61, default).Returns(update);
        fixture.Identity.GetGuest(1001)
            .Returns(new AgentDelegatedIdentity(guestId, "TelegramGuest", "guest-token"));
        fixture.UserLinks.GetByTelegramUserIdAsync(1001, default)
            .Returns(
                (TelegramUserLink?)null,
                userLink);
        fixture.ConversationLinks.GetBindingAsync(userLink.Id, default)
            .Returns(new TelegramConversationBinding(
                ConversationId, false, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(-5)));
        fixture.Context.ResolveAsync(
                PersonId,
                ConversationId,
                "telegram-update-61-resume",
                "Telegram",
                default)
            .Returns(new AgentConversationContext(ConversationId, "web", false));
        fixture.Identity.GetAsync(PersonId, default)
            .Returns(new AgentDelegatedIdentity(PersonId, "Cliente", "delegated-token"));
        fixture.Dispatcher.DispatchAsync(
                Arg.Any<AgentMessageDispatchRequest>(),
                Arg.Any<AgentConversationContext>(),
                "guest-token",
                default)
            .Returns(Result(
                "¡Listo, Ana! Guardé tu registro. Continúo con lo que estabas haciendo.",
                AgentAccessRequirement.None,
                "Quiero agendar una cita para Medicina interna"));
        fixture.Dispatcher.DispatchAsync(
                Arg.Is<AgentMessageDispatchRequest>(request =>
                    request.Message == "Quiero agendar una cita para Medicina interna" &&
                    request.Role == "Cliente" &&
                    request.PersonId == PersonId),
                Arg.Any<AgentConversationContext>(),
                "delegated-token",
                default)
            .Returns(Result("¿Para cuál mascota quieres la cita?"));

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(61), default);

        await fixture.Bot.Received(1).SendTextAsync(
            1001,
            Arg.Is<string>(text =>
                text.Contains("Guardé tu registro", StringComparison.Ordinal) &&
                text.Contains("¿Para cuál mascota", StringComparison.Ordinal)),
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
                text.Contains("generales", StringComparison.OrdinalIgnoreCase) &&
                !text.Contains("/vincular", StringComparison.OrdinalIgnoreCase) &&
                !text.Contains("/registrar", StringComparison.OrdinalIgnoreCase) &&
                !text.Contains("código", StringComparison.OrdinalIgnoreCase)),
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
        fixture.ConversationLinks.GetBindingAsync(userLink.Id, default)
            .Returns(new TelegramConversationBinding(ConversationId, false, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(-5)));
        fixture.Context.ResolveAsync(PersonId, ConversationId, "telegram-update-43-verified", "Telegram", default)
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
    public async Task Linked_user_message_is_persisted_as_a_client_chat_message()
    {
        var fixture = CreateFixture();
        var update = ProcessingUpdate(80, "¿Qué vacunas necesita?");
        var userLink = TelegramUserLink.Create(PersonId, 1001, 1001, Now.UtcDateTime);
        fixture.Updates.GetByIdAsync(80, default).Returns(update);
        fixture.UserLinks.GetByTelegramUserIdAsync(1001, default).Returns(userLink);
        fixture.ConversationLinks.GetBindingAsync(userLink.Id, default)
            .Returns(new TelegramConversationBinding(ConversationId, false, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(-5)));
        fixture.Context.ResolveAsync(PersonId, ConversationId, "telegram-update-80-verified", "Telegram", default)
            .Returns(new AgentConversationContext(ConversationId, "web", false));
        fixture.Identity.GetAsync(PersonId, default)
            .Returns(new AgentDelegatedIdentity(PersonId, "Cliente", "delegated-token"));
        fixture.Dispatcher.DispatchAsync(
                Arg.Any<AgentMessageDispatchRequest>(),
                Arg.Any<AgentConversationContext>(),
                "delegated-token",
                default)
            .Returns(Result("Respuesta veterinaria"));

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(80), default);

        await fixture.Sender.Received(1).Send(
            Arg.Is<GetChatParticipantsByConversationIdQuery>(query =>
                query.ChatConversationId == ConversationId),
            default);
        await fixture.Sender.Received(1).Send(
            Arg.Is<CreateChatMessageCommand>(command =>
                command.ChatConversationId == ConversationId &&
                command.SenderTypesId == ClientParticipantTypeId &&
                command.MessageTypeId == TextMessageTypeId &&
                command.Content == "¿Qué vacunas necesita?" &&
                command.Metadata == null),
            default);
        // Ticket B3, Decisión 2: la respuesta de la IA no se persiste en esta ronda.
        await fixture.Sender.DidNotReceive().Send(
            Arg.Is<CreateChatMessageCommand>(command => command.Content == "Respuesta veterinaria"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_client_participant_skips_persistence_without_failing_the_turn()
    {
        var fixture = CreateFixture();
        var update = ProcessingUpdate(81, "hola de nuevo");
        var userLink = TelegramUserLink.Create(PersonId, 1001, 1001, Now.UtcDateTime);
        fixture.Updates.GetByIdAsync(81, default).Returns(update);
        fixture.UserLinks.GetByTelegramUserIdAsync(1001, default).Returns(userLink);
        fixture.ConversationLinks.GetBindingAsync(userLink.Id, default)
            .Returns(new TelegramConversationBinding(ConversationId, false, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(-5)));
        fixture.Context.ResolveAsync(PersonId, ConversationId, "telegram-update-81-verified", "Telegram", default)
            .Returns(new AgentConversationContext(ConversationId, "web", false));
        fixture.Identity.GetAsync(PersonId, default)
            .Returns(new AgentDelegatedIdentity(PersonId, "Cliente", "delegated-token"));
        fixture.Dispatcher.DispatchAsync(
                Arg.Any<AgentMessageDispatchRequest>(),
                Arg.Any<AgentConversationContext>(),
                "delegated-token",
                default)
            .Returns(Result("Hola de nuevo"));
        // Sin participante "Cliente" para esta conversación (caso inesperado).
        fixture.Sender.Send(
                Arg.Is<GetChatParticipantsByConversationIdQuery>(query =>
                    query.ChatConversationId == ConversationId),
                default)
            .Returns((IReadOnlyCollection<ChatParticipantEntity>)Array.Empty<ChatParticipantEntity>());

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(81), default);

        await fixture.Sender.DidNotReceive().Send(
            Arg.Any<CreateChatMessageCommand>(),
            Arg.Any<CancellationToken>());
        // El turno sigue su curso normal aunque no se pudo persistir el mensaje.
        await fixture.Bot.Received(1).SendTextAsync(1001, "Hola de nuevo", default);
    }

    [Theory]
    [InlineData("Quiero hablar con un asesor")]
    [InlineData("hablar con alguien porfa")]
    [InlineData("necesito un humano")]
    public async Task Linked_user_escalation_phrase_creates_a_pending_escalation_and_skips_the_agent(
        string message)
    {
        var fixture = CreateFixture();
        var update = ProcessingUpdate(70, message);
        var userLink = TelegramUserLink.Create(PersonId, 1001, 1001, Now.UtcDateTime);
        fixture.Updates.GetByIdAsync(70, default).Returns(update);
        fixture.UserLinks.GetByTelegramUserIdAsync(1001, default).Returns(userLink);
        fixture.ConversationLinks.GetBindingAsync(userLink.Id, default)
            .Returns(new TelegramConversationBinding(ConversationId, false, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(-5)));
        fixture.Context.ResolveAsync(
                PersonId,
                ConversationId,
                "telegram-update-70-verified",
                "Telegram",
                default)
            .Returns(new AgentConversationContext(ConversationId, "web", false));

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(70), default);

        await fixture.Sender.Received(1).Send(
            Arg.Is<CreateChatEscalationCommand>(command =>
                command.ChatConversationId == ConversationId &&
                command.EscalationStatusId == PendingEscalationStatusId &&
                command.FromAi == false &&
                command.Reason == message),
            default);
        // Ticket B3: el propio mensaje que disparó el escalamiento también queda
        // guardado — la Recepcionista necesita verlo, no solo el resumen en Reason.
        await fixture.Sender.Received(1).Send(
            Arg.Is<CreateChatMessageCommand>(command =>
                command.ChatConversationId == ConversationId &&
                command.SenderTypesId == ClientParticipantTypeId &&
                command.MessageTypeId == TextMessageTypeId &&
                command.Content == message),
            default);
        await fixture.Dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<AgentMessageDispatchRequest>(),
            Arg.Any<AgentConversationContext>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await fixture.Bot.Received(1).SendTextAsync(
            1001,
            "Tu conversación está siendo atendida por un asesor.",
            default);
    }

    [Fact]
    public async Task Linked_user_repeating_the_escalation_phrase_does_not_duplicate_the_escalation()
    {
        var fixture = CreateFixture();
        var update = ProcessingUpdate(71, "asesor por favor otra vez");
        var userLink = TelegramUserLink.Create(PersonId, 1001, 1001, Now.UtcDateTime);
        fixture.Updates.GetByIdAsync(71, default).Returns(update);
        fixture.UserLinks.GetByTelegramUserIdAsync(1001, default).Returns(userLink);
        fixture.ConversationLinks.GetBindingAsync(userLink.Id, default)
            .Returns(new TelegramConversationBinding(ConversationId, false, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(-5)));
        // Ya hay un escalamiento activo sin resolver: IConversationContextProvider ya lo
        // refleja en IsEscalated (mismo IActiveConversationEscalationReader que usa
        // PersistentConversationContextProvider).
        fixture.Context.ResolveAsync(
                PersonId,
                ConversationId,
                "telegram-update-71-verified",
                "Telegram",
                default)
            .Returns(new AgentConversationContext(ConversationId, "web", true));

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(71), default);

        await fixture.Sender.DidNotReceive().Send(
            Arg.Any<CreateChatEscalationCommand>(),
            Arg.Any<CancellationToken>());
        await fixture.Dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<AgentMessageDispatchRequest>(),
            Arg.Any<AgentConversationContext>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await fixture.Bot.DidNotReceive().SendTextAsync(
            Arg.Any<long>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        Assert.Equal(TelegramInboundUpdateStatus.Completed, update.Status);
    }

    [Fact]
    public async Task Linked_user_message_during_active_escalation_is_persisted_without_bot_reply()
    {
        const string message = "Necesito agregar otro detalle";
        var fixture = CreateFixture();
        var update = ProcessingUpdate(73, message);
        var userLink = TelegramUserLink.Create(PersonId, 1001, 1001, Now.UtcDateTime);
        fixture.Updates.GetByIdAsync(73, default).Returns(update);
        fixture.UserLinks.GetByTelegramUserIdAsync(1001, default).Returns(userLink);
        fixture.ConversationLinks.GetBindingAsync(userLink.Id, default)
            .Returns(new TelegramConversationBinding(ConversationId, false, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(-5)));
        fixture.Context.ResolveAsync(
                PersonId,
                ConversationId,
                "telegram-update-73-verified",
                "Telegram",
                default)
            .Returns(new AgentConversationContext(ConversationId, "web", true));
        fixture.Identity.GetAsync(PersonId, default)
            .Returns(new AgentDelegatedIdentity(PersonId, "Cliente", "delegated-token"));
        fixture.Dispatcher.DispatchAsync(
                Arg.Any<AgentMessageDispatchRequest>(),
                Arg.Any<AgentConversationContext>(),
                "delegated-token",
                default)
            .Returns(Result("Tu conversación está siendo atendida por un asesor."));

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(73), default);

        await fixture.Sender.Received(1).Send(
            Arg.Is<CreateChatMessageCommand>(command =>
                command.ChatConversationId == ConversationId &&
                command.SenderTypesId == ClientParticipantTypeId &&
                command.MessageTypeId == TextMessageTypeId &&
                command.Content == message),
            default);
        await fixture.Sender.DidNotReceive().Send(
            Arg.Any<CreateChatEscalationCommand>(),
            Arg.Any<CancellationToken>());
        await fixture.Dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<AgentMessageDispatchRequest>(),
            Arg.Any<AgentConversationContext>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await fixture.Bot.DidNotReceive().SendTextAsync(
            Arg.Any<long>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        Assert.Equal(TelegramInboundUpdateStatus.Completed, update.Status);
    }

    [Theory]
    [InlineData("asesor")]
    [InlineData("quiero hablar con una persona")]
    public async Task Unlinked_guest_escalation_phrase_is_redirected_without_touching_escalations(
        string message)
    {
        var fixture = CreateFixture();
        var update = ProcessingUpdate(72, message);
        fixture.Settings.GuestModeEnabled.Returns(true);
        fixture.Updates.GetByIdAsync(72, default).Returns(update);
        fixture.UserLinks.GetByTelegramUserIdAsync(1001, default)
            .Returns((TelegramUserLink?)null);

        await fixture.Handler.Handle(new ProcessTelegramUpdateCommand(72), default);

        await fixture.Sender.DidNotReceive().Send(
            Arg.Any<CreateChatEscalationCommand>(),
            Arg.Any<CancellationToken>());
        await fixture.ConversationLinks.DidNotReceive().AddAsync(
            Arg.Any<TelegramConversationLink>(),
            Arg.Any<CancellationToken>());
        await fixture.ConversationLinks.DidNotReceive().UpdateAsync(
            Arg.Any<TelegramConversationLink>(),
            Arg.Any<CancellationToken>());
        await fixture.Dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<AgentMessageDispatchRequest>(),
            Arg.Any<AgentConversationContext>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await fixture.Bot.Received(1).SendTextAsync(
            1001,
            Arg.Is<string>(text =>
                text.Contains("identificarte", StringComparison.OrdinalIgnoreCase) &&
                !text.Contains("cédula", StringComparison.OrdinalIgnoreCase)),
            default);
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
        fixture.ConversationLinks.GetBindingAsync(userLink.Id, default)
            .Returns(new TelegramConversationBinding(ConversationId, false, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(-5)));
        fixture.Context.ResolveAsync(PersonId, ConversationId, "telegram-update-52-verified", "Telegram", default)
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
        var conversationDefaults = Substitute.For<IAgentConversationDefaults>();
        conversationDefaults.ClientParticipantTypeId.Returns(ClientParticipantTypeId);
        var bot = Substitute.For<ITelegramBotClient>();
        var sender = Substitute.For<ISender>();
        var settings = Substitute.For<ITelegramRuntimeSettings>();
        settings.MaxProcessingAttempts.Returns(3);
        settings.PendingEscalationStatusId.Returns(PendingEscalationStatusId);
        settings.TextMessageTypeId.Returns(TextMessageTypeId);
        var logger = new RecordingLogger<ProcessTelegramUpdateHandler>();

        // Por defecto, toda conversación consultada ya tiene su participante
        // "Cliente" (así lo crea PersistentConversationContextProvider) — los
        // tests que necesiten simular su ausencia lo sobrescriben explícitamente.
        sender.Send(Arg.Any<GetChatParticipantsByConversationIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var query = callInfo.Arg<GetChatParticipantsByConversationIdQuery>();
                IReadOnlyCollection<ChatParticipantEntity> participants =
                [
                    ChatParticipantEntity.Create(
                        query.ChatConversationId,
                        ClientParticipantTypeId,
                        clientId: Guid.NewGuid())
                ];
                return participants;
            });

        return new Fixture(
            new ProcessTelegramUpdateHandler(
                unitOfWork,
                context,
                dispatcher,
                identity,
                conversationDefaults,
                bot,
                sender,
                settings,
                new FixedTimeProvider(currentTime ?? Now),
                logger),
            updates,
            userLinks,
            conversationLinks,
            context,
            dispatcher,
            identity,
            conversationDefaults,
            bot,
            sender,
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
        AgentAccessRequirement accessRequirement = AgentAccessRequirement.None,
        string? resumeMessage = null) =>
        new(message, ConversationId, Guid.NewGuid(), "ai_generated", "openai", "gpt", null, null,
            new AgentRagResult("used", "contextual", 0.9, 1, 1, true, false),
            accessRequirement,
            resumeMessage);

    private sealed record Fixture(
        ProcessTelegramUpdateHandler Handler,
        ITelegramInboundUpdateRepository Updates,
        ITelegramUserLinkRepository UserLinks,
        ITelegramConversationLinkRepository ConversationLinks,
        IConversationContextProvider Context,
        IAgentMessageDispatcher Dispatcher,
        IAgentDelegatedIdentityProvider Identity,
        IAgentConversationDefaults ConversationDefaults,
        ITelegramBotClient Bot,
        ISender Sender,
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
