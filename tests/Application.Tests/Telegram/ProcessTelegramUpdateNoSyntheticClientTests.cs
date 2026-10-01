using Application.Agent.Abstractions;
using Application.Agent.Messages;
using Application.Clients.Abstraction;
using Application.Telegram.Abstractions;
using Application.Telegram.Models;
using Application.Telegram.Processing;
using Domain.Telegram.Entities;
using Domain.Telegram.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Application.Tests.Telegram;

// Ticket 3 P1: guest Telegram no debe persistir CLIENTS ni fabricar teléfonos.
public sealed class ProcessTelegramUpdateNoSyntheticClientTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 31, 18, 0, 0, TimeSpan.Zero);
    private static readonly Guid GuestId =
        Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid PendingEscalationStatusId =
        Guid.Parse("85000000-0000-0000-0000-000000000001");
    private static readonly Guid ClientParticipantTypeId =
        Guid.Parse("82000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Guest_dispatch_does_not_create_lookup_or_mutate_clients()
    {
        var unitOfWork = Substitute.For<ITelegramUnitOfWork>();
        var updates = Substitute.For<ITelegramInboundUpdateRepository>();
        var userLinks = Substitute.For<ITelegramUserLinkRepository>();
        var clients = Substitute.For<IClientRepository>();
        unitOfWork.InboundUpdatesRepository.Returns(updates);
        unitOfWork.UserLinksRepository.Returns(userLinks);
        unitOfWork.ClientsRepository.Returns(clients);

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
        settings.GuestModeEnabled.Returns(true);

        var update = TelegramInboundUpdate.Create(
            90, 1001, 1001, 7, "private", "¿Horario de atención?", Now.UtcDateTime);
        update.Claim(Now.UtcDateTime);
        updates.GetByIdAsync(90, default).Returns(update);
        userLinks.GetByTelegramUserIdAsync(1001, default).Returns((TelegramUserLink?)null);
        identity.GetGuest(1001)
            .Returns(new AgentDelegatedIdentity(GuestId, "TelegramGuest", "guest-token"));
        dispatcher.DispatchAsync(
                Arg.Any<AgentMessageDispatchRequest>(),
                Arg.Any<AgentConversationContext>(),
                "guest-token",
                default)
            .Returns(new AgentMessageResult(
                "Respondemos en horario de clínica.",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "ai_generated",
                "openai",
                "gpt",
                null,
                null,
                new AgentRagResult("used", "contextual", 0.9, 1, 1, true, false),
                AgentAccessRequirement.None,
                null));

        var handler = new ProcessTelegramUpdateHandler(
            unitOfWork,
            context,
            dispatcher,
            identity,
            conversationDefaults,
            bot,
            sender,
            settings,
            new FixedTimeProvider(Now),
            Substitute.For<ILogger<ProcessTelegramUpdateHandler>>());

        await handler.Handle(new ProcessTelegramUpdateCommand(90), default);

        await clients.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await clients.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await clients.DidNotReceiveWithAnyArgs().GetByPhoneAsync(default!, default);
        await clients.DidNotReceiveWithAnyArgs()
            .ExistsByPhoneAsync(default!, default, default);
        await context.DidNotReceiveWithAnyArgs()
            .ResolveAsync(default, default, default!, default!, default);
        identity.Received(1).GetGuest(1001);
        await identity.DidNotReceiveWithAnyArgs().GetAsync(default, default);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
