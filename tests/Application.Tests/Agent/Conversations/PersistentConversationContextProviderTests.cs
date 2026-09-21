using Application.Agent.Abstractions;
using Application.Agent.Conversations;
using Application.Agent.Errors;
using Application.Clients.Abstraction;
using Application.ChatConversations.Abstraction;
using Application.ChatParticipants.Abstraction;
using Application.Common.Abstractions;
using Application.ConversationStatuses.Abstraction;
using Application.SenderTypes.Abstraction;
using Domain.ChatConversations.Entities;
using Domain.ChatParticipants.Entities;
using Domain.ConversationStatuses.Entities;
using Domain.SenderTypes.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Agent.Conversations;

public sealed class PersistentConversationContextProviderTests
{
    private static readonly Guid PersonId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid InitialStatusId = Guid.Parse("81000000-0000-0000-0000-000000000001");
    private static readonly Guid ClientTypeId = Guid.Parse("82000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Resolve_without_conversation_creates_conversation_and_client_participant_once()
    {
        var fixture = CreateFixture();
        var provider = fixture.CreateProvider();

        var result = await provider.ResolveAsync(
            PersonId,
            null,
            "message-001",
            "Web",
            fixture.Token);

        Assert.NotEqual(Guid.Empty, result.ConversationId);
        Assert.Equal("web", result.Channel);
        Assert.False(result.IsEscalated);
        await fixture.Conversations.Received(1).AddAsync(
            Arg.Is<ChatConversation>(conversation =>
                conversation.Id == result.ConversationId &&
                conversation.ConversationStatusId == InitialStatusId &&
                conversation.AiEnabled),
            fixture.Token);
        await fixture.Participants.Received(1).AddAsync(
            Arg.Is<ChatParticipant>(participant =>
                participant.ChatConversationId == result.ConversationId &&
                participant.ParticipantTypeId == ClientTypeId &&
                participant.ClientId == PersonId &&
                participant.AgentHumanId == null),
            fixture.Token);
        await fixture.UnitOfWork.Received(1).SaveChangesAsync(fixture.Token);
    }

    [Fact]
    public async Task Resolve_without_conversation_persists_the_requested_channel()
    {
        // Ticket B6: la conversación creada guarda el canal que pidió quien
        // resuelve el contexto (Telegram, en este caso) — antes no existía
        // ningún campo persistido para esto.
        var fixture = CreateFixture();
        var provider = fixture.CreateProvider();

        await provider.ResolveAsync(
            PersonId,
            null,
            "message-001-telegram",
            "Telegram",
            fixture.Token);

        await fixture.Conversations.Received(1).AddAsync(
            Arg.Is<ChatConversation>(conversation => conversation.Channel == "Telegram"),
            fixture.Token);
    }

    [Fact]
    public async Task Resolve_without_conversation_never_touches_users_to_identify_the_client()
    {
        // T9: el participante se identifica solo con CLIENTS; no hace falta
        // ninguna fila en USERS ni en un perfil de chat.
        var fixture = CreateFixture();
        var provider = fixture.CreateProvider();

        var result = await provider.ResolveAsync(
            PersonId,
            null,
            "message-002",
            "Web",
            fixture.Token);

        _ = fixture.UnitOfWork.DidNotReceive().UsersRepository;
        await fixture.Participants.Received(1).AddAsync(
            Arg.Is<ChatParticipant>(participant =>
                participant.ChatConversationId == result.ConversationId &&
                participant.ClientId == PersonId),
            fixture.Token);
        await fixture.UnitOfWork.Received(1).SaveChangesAsync(fixture.Token);
    }

    [Fact]
    public async Task Resolve_without_conversation_rejects_a_missing_initial_status()
    {
        var fixture = CreateFixture();
        fixture.UnitOfWork.ConversationStatusesRepository
            .GetByIdAsync(InitialStatusId, fixture.Token)
            .Returns(Task.FromResult<ConversationStatusEntity?>(null));
        var provider = fixture.CreateProvider();

        await Assert.ThrowsAsync<AgentConversationConfigurationException>(async () =>
            await provider.ResolveAsync(
                PersonId,
                null,
                "message-config",
                "Web",
                fixture.Token));

        await fixture.UnitOfWork.DidNotReceive().SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolve_existing_conversation_allows_the_client_participant()
    {
        var fixture = CreateFixture();
        var conversation = ChatConversation.Create(InitialStatusId);
        var participant = ChatParticipant.Create(
            conversation.Id,
            ClientTypeId,
            clientId: PersonId);
        fixture.ConfigureExistingConversation(conversation, [participant]);
        var provider = fixture.CreateProvider();

        var result = await provider.ResolveAsync(
            PersonId,
            conversation.Id,
            "message-003",
            "Web",
            fixture.Token);

        Assert.Equal(conversation.Id, result.ConversationId);
        Assert.False(result.IsEscalated);
        await fixture.UnitOfWork.DidNotReceive().SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolve_existing_conversation_rejects_a_missing_conversation()
    {
        var fixture = CreateFixture();
        var missingId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        fixture.Conversations.GetByIdAsync(missingId, fixture.Token)
            .Returns(Task.FromResult<ChatConversation?>(null));
        var provider = fixture.CreateProvider();

        await Assert.ThrowsAsync<AgentConversationNotFoundException>(async () =>
            await provider.ResolveAsync(
                PersonId,
                missingId,
                "message-004",
                "Web",
                fixture.Token));
    }

    [Fact]
    public async Task Resolve_existing_conversation_rejects_a_non_participant()
    {
        var fixture = CreateFixture();
        var conversation = ChatConversation.Create(InitialStatusId);
        fixture.ConfigureExistingConversation(conversation, []);
        var provider = fixture.CreateProvider();

        await Assert.ThrowsAsync<AgentConversationForbiddenException>(async () =>
            await provider.ResolveAsync(
                PersonId,
                conversation.Id,
                "message-005",
                "Web",
                fixture.Token));
    }

    [Fact]
    public async Task Resolve_existing_conversation_rejects_a_deleted_client()
    {
        var fixture = CreateFixture();
        var conversation = ChatConversation.Create(InitialStatusId);
        var participant = ChatParticipant.Create(
            conversation.Id,
            ClientTypeId,
            clientId: PersonId);
        fixture.ConfigureExistingConversation(conversation, [participant]);
        fixture.UnitOfWork.ClientsRepository.GetByIdAsync(PersonId, fixture.Token)
            .Returns(Task.FromResult<Domain.Clients.Entities.ClientEntity?>(null));
        var provider = fixture.CreateProvider();

        await Assert.ThrowsAsync<AgentConversationForbiddenException>(async () =>
            await provider.ResolveAsync(
                PersonId,
                conversation.Id,
                "message-user-missing",
                "Web",
                fixture.Token));
    }

    [Fact]
    public async Task Resolve_existing_conversation_marks_an_unresolved_escalation()
    {
        var fixture = CreateFixture();
        var conversation = ChatConversation.Create(InitialStatusId);
        var participant = ChatParticipant.Create(
            conversation.Id,
            ClientTypeId,
            clientId: PersonId);
        fixture.ConfigureExistingConversation(conversation, [participant]);
        fixture.EscalationReader.HasActiveAsync(conversation.Id, fixture.Token)
            .Returns(Task.FromResult(true));
        var provider = fixture.CreateProvider();

        var result = await provider.ResolveAsync(
            PersonId,
            conversation.Id,
            "message-006",
            "Web",
            fixture.Token);

        Assert.True(result.IsEscalated);
    }

    [Fact]
    public async Task Resolve_existing_conversation_ignores_a_resolved_escalation()
    {
        var fixture = CreateFixture();
        var conversation = ChatConversation.Create(InitialStatusId);
        var participant = ChatParticipant.Create(
            conversation.Id,
            ClientTypeId,
            clientId: PersonId);
        fixture.ConfigureExistingConversation(conversation, [participant]);
        fixture.EscalationReader.HasActiveAsync(conversation.Id, fixture.Token)
            .Returns(Task.FromResult(false));
        var provider = fixture.CreateProvider();

        var result = await provider.ResolveAsync(
            PersonId,
            conversation.Id,
            "message-007",
            "Web",
            fixture.Token);

        Assert.False(result.IsEscalated);
    }

    private static Fixture CreateFixture()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var conversations = Substitute.For<IChatConversationRepository>();
        var participants = Substitute.For<IChatParticipantRepository>();
        var statuses = Substitute.For<IConversationStatusRepository>();
        var senderTypes = Substitute.For<ISenderTypeRepository>();
        var escalationReader = Substitute.For<IActiveConversationEscalationReader>();
        var token = new CancellationTokenSource().Token;

        var clients = Substitute.For<IClientRepository>();
        unitOfWork.ClientsRepository.Returns(clients);
        unitOfWork.ChatConversationsRepository.Returns(conversations);
        unitOfWork.ChatParticipantsRepository.Returns(participants);
        unitOfWork.ConversationStatusesRepository.Returns(statuses);
        unitOfWork.SenderTypesRepository.Returns(senderTypes);
        clients.GetByIdAsync(PersonId, token)
            .Returns(Task.FromResult<Domain.Clients.Entities.ClientEntity?>(
                new Domain.Clients.Entities.ClientEntity(
                    "Samuel Calderón",
                    "samuel@example.test",
                    "1234567890",
                    "3001234567",
                    "Calle 123")));
        statuses.GetByIdAsync(InitialStatusId, token)
            .Returns(Task.FromResult<ConversationStatusEntity?>(new ConversationStatusEntity("Abierta")));
        senderTypes.GetByIdAsync(ClientTypeId, token)
            .Returns(Task.FromResult<SenderTypeEntity?>(new SenderTypeEntity("Cliente")));
        unitOfWork.SaveChangesAsync(token).Returns(Task.FromResult(3));

        return new Fixture(
            unitOfWork,
            conversations,
            participants,
            escalationReader,
            token);
    }

    private sealed record Defaults : IAgentConversationDefaults
    {
        public Guid InitialConversationStatusId => InitialStatusId;
        public Guid ClientParticipantTypeId => ClientTypeId;
    }

    private sealed record Fixture(
        IUnitOfWork UnitOfWork,
        IChatConversationRepository Conversations,
        IChatParticipantRepository Participants,
        IActiveConversationEscalationReader EscalationReader,
        CancellationToken Token)
    {
        public PersistentConversationContextProvider CreateProvider() =>
            new(UnitOfWork, new Defaults(), EscalationReader);

        public void ConfigureExistingConversation(
            ChatConversation conversation,
            IReadOnlyCollection<ChatParticipant> participants)
        {
            Conversations.GetByIdAsync(conversation.Id, Token)
                .Returns(Task.FromResult<ChatConversation?>(conversation));
            Participants.GetAllByConversationIdAsync(conversation.Id, Token)
                .Returns(Task.FromResult(participants));
            EscalationReader.HasActiveAsync(conversation.Id, Token)
                .Returns(Task.FromResult(false));
        }
    }
}
