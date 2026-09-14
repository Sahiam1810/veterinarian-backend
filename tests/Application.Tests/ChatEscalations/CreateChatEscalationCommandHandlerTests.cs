using Application.Agent.Abstractions;
using Application.ChatEscalations.UseCase;
using Application.ChatParticipants.Abstraction;
using Application.ChatUserProfiles.Abstraction;
using Application.Clients.Abstraction;
using Application.Common.Abstractions;
using Application.Notifications.Abstraction;
using Application.Telegram.Abstractions;
using Application.Users.Abstraction;
using Domain.ChatConversations.Entities;
using Domain.ChatParticipants.Entities;
using Domain.ChatUserProfiles.Entities;
using Domain.Clients.Entities;
using Domain.EscalationStatuses.Entities;
using Domain.Telegram.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.ChatEscalations;

public sealed class CreateChatEscalationCommandHandlerTests
{
    private static readonly Guid ConversationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid EscalationStatusId = Guid.Parse("85000000-0000-0000-0000-000000000001");
    private static readonly Guid ClientParticipantTypeId = Guid.Parse("82000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Creating_an_escalation_broadcasts_the_created_event_with_status_name()
    {
        var fixture = CreateFixture();
        fixture.Uow.ChatConversationsRepository
            .GetByIdAsync(ConversationId, default)
            .Returns(ChatConversation.Create(Guid.NewGuid()));
        fixture.Uow.EscalationStatusesRepository
            .GetByIdAsync(EscalationStatusId, default)
            .Returns(new EscalationStatusEntity("Pendiente"));
        fixture.ConversationLinks
            .GetByConversationIdAsync(ConversationId, default)
            .Returns((TelegramConversationLink?)null);
        fixture.Uow.ChatParticipantsRepository
            .GetAllByConversationIdAsync(ConversationId, default)
            .Returns((IReadOnlyCollection<ChatParticipant>)Array.Empty<ChatParticipant>());

        var escalation = await fixture.Handler.Handle(
            new CreateChatEscalationCommand(ConversationId, EscalationStatusId, false, "asesor", null),
            default);

        await fixture.ChatRealtimeNotifier.Received(1).NotifyEscalationCreatedAsync(
            Arg.Is<ChatEscalationCreatedPayload>(payload =>
                payload.EscalationId == escalation.Id &&
                payload.ConversationId == ConversationId &&
                payload.Status == "Pendiente" &&
                payload.Reason == "asesor" &&
                payload.LastMessage == "asesor" &&
                payload.Channel == null &&
                payload.ClientId == null),
            default);
    }

    [Fact]
    public async Task Escalation_for_a_telegram_linked_conversation_reports_telegram_channel()
    {
        var fixture = CreateFixture();
        fixture.Uow.ChatConversationsRepository
            .GetByIdAsync(ConversationId, default)
            .Returns(ChatConversation.Create(Guid.NewGuid()));
        fixture.Uow.EscalationStatusesRepository
            .GetByIdAsync(EscalationStatusId, default)
            .Returns(new EscalationStatusEntity("Pendiente"));
        fixture.ConversationLinks
            .GetByConversationIdAsync(ConversationId, default)
            .Returns(TelegramConversationLink.Create(Guid.NewGuid(), ConversationId, DateTime.UtcNow));
        fixture.Uow.ChatParticipantsRepository
            .GetAllByConversationIdAsync(ConversationId, default)
            .Returns((IReadOnlyCollection<ChatParticipant>)Array.Empty<ChatParticipant>());

        await fixture.Handler.Handle(
            new CreateChatEscalationCommand(ConversationId, EscalationStatusId, false, "asesor", null),
            default);

        await fixture.ChatRealtimeNotifier.Received(1).NotifyEscalationCreatedAsync(
            Arg.Is<ChatEscalationCreatedPayload>(payload => payload.Channel == "Telegram"),
            default);
    }

    [Fact]
    public async Task Escalation_resolves_client_name_and_phone_from_the_linked_participant()
    {
        var fixture = CreateFixture();
        var userId = Guid.NewGuid();
        var profile = ChatUserProfile.Create(userId, null, null, null);
        var clientParticipant = ChatParticipant.Create(
            ConversationId, ClientParticipantTypeId, chatUserProfileId: profile.Id);
        var client = new ClientEntity(userId, "1234567890", null, phoneNumber: "3001234567");
        var user = new UserEntity("Ana Pérez", "ana@example.test", null, Guid.NewGuid());

        fixture.Uow.ChatConversationsRepository
            .GetByIdAsync(ConversationId, default)
            .Returns(ChatConversation.Create(Guid.NewGuid()));
        fixture.Uow.EscalationStatusesRepository
            .GetByIdAsync(EscalationStatusId, default)
            .Returns(new EscalationStatusEntity("Pendiente"));
        fixture.ConversationLinks
            .GetByConversationIdAsync(ConversationId, default)
            .Returns((TelegramConversationLink?)null);
        fixture.Uow.ChatParticipantsRepository
            .GetAllByConversationIdAsync(ConversationId, default)
            .Returns((IReadOnlyCollection<ChatParticipant>)new[] { clientParticipant });
        fixture.Uow.ChatUserProfilesRepository
            .GetByIdAsync(profile.Id, default)
            .Returns(profile);
        fixture.Uow.ClientsRepository
            .GetByUserIdAsync(userId, default)
            .Returns(client);
        fixture.Uow.UsersRepository
            .GetByIdAsync(userId, default)
            .Returns(user);

        await fixture.Handler.Handle(
            new CreateChatEscalationCommand(ConversationId, EscalationStatusId, false, "asesor", null),
            default);

        await fixture.ChatRealtimeNotifier.Received(1).NotifyEscalationCreatedAsync(
            Arg.Is<ChatEscalationCreatedPayload>(payload =>
                payload.ClientId == client.Id &&
                payload.ClientName == "Ana Pérez" &&
                payload.ClientPhone == "3001234567"),
            default);
    }

    [Fact]
    public async Task Broadcast_failure_does_not_prevent_escalation_creation()
    {
        var fixture = CreateFixture();
        fixture.Uow.ChatConversationsRepository
            .GetByIdAsync(ConversationId, default)
            .Returns(ChatConversation.Create(Guid.NewGuid()));
        fixture.Uow.EscalationStatusesRepository
            .GetByIdAsync(EscalationStatusId, default)
            .Returns(new EscalationStatusEntity("Pendiente"));
        fixture.ConversationLinks
            .GetByConversationIdAsync(ConversationId, default)
            .Returns((TelegramConversationLink?)null);
        fixture.Uow.ChatParticipantsRepository
            .GetAllByConversationIdAsync(ConversationId, default)
            .Returns((IReadOnlyCollection<ChatParticipant>)Array.Empty<ChatParticipant>());
        fixture.ChatRealtimeNotifier
            .NotifyEscalationCreatedAsync(Arg.Any<ChatEscalationCreatedPayload>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("hub unavailable"));

        var escalation = await fixture.Handler.Handle(
            new CreateChatEscalationCommand(ConversationId, EscalationStatusId, false, "asesor", null),
            default);

        Assert.NotNull(escalation);
    }

    private static Fixture CreateFixture()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var conversationDefaults = Substitute.For<IAgentConversationDefaults>();
        conversationDefaults.ClientParticipantTypeId.Returns(ClientParticipantTypeId);
        var conversationLinks = Substitute.For<ITelegramConversationLinkRepository>();
        var chatRealtimeNotifier = Substitute.For<IChatRealtimeNotifier>();
        var logger = Substitute.For<ILogger<CreateChatEscalationCommandHandler>>();

        return new Fixture(
            new CreateChatEscalationCommandHandler(
                uow, conversationDefaults, conversationLinks, chatRealtimeNotifier, logger),
            uow,
            conversationLinks,
            chatRealtimeNotifier);
    }

    private sealed record Fixture(
        CreateChatEscalationCommandHandler Handler,
        IUnitOfWork Uow,
        ITelegramConversationLinkRepository ConversationLinks,
        IChatRealtimeNotifier ChatRealtimeNotifier);
}
