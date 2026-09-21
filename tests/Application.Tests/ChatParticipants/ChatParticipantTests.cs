using Application.AccountStatements.Abstraction;
using Application.AgentHumans.Abstraction;
using Application.Appointments.Abstraction;
using Application.AppointmentStatusHistories.Abstraction;
using Application.Availabilities.Abstraction;
using Application.ChatConversations.Abstraction;
using Application.ChatEscalationResolutions.Abstraction;
using Application.ChatEscalations.Abstraction;
using Application.ChatMessages.Abstraction;
using Application.ChatParticipants.Abstraction;
using Application.ChatParticipants.UseCase;
using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Tests.Common;
using Application.ConversationStatuses.Abstraction;
using Application.Diagnostics.Abstraction;
using Application.EscalationStatuses.Abstraction;
using Application.MedicalRecords.Abstraction;
using Application.MessageTypes.Abstraction;
using Application.Notifications.Abstraction;
using Application.Pets.Abstraction;
using Application.Priorities.Abstraction;
using Application.Races.Abstraction;
using Application.Roles.Abstraction;
using Application.SenderTypes.Abstraction;
using Application.Services.Abstraction;
using Application.Specialties.Abstraction;
using Application.Species.Abstraction;
using Application.StatusAppointments.Abstraction;
using Application.TypeServices.Abstraction;
using Application.UserAccounts.Abstraction;
using Application.UserCredentials.Abstraction;
using Application.Users.Abstraction;
using Application.UserTokens.Abstraction;
using Application.Vaccinations.Abstraction;
using Application.Veterinarians.Abstraction;
using Domain.AgentHumans.Entities;
using Domain.ChatConversations.Entities;
using Domain.ChatParticipants.Entities;
using Domain.Clients.Entities;
using Domain.SenderTypes.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.ChatParticipants;

public sealed class ChatParticipantTests
{
    private static readonly Guid ValidConversationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ValidParticipantTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ValidClientId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ValidAgentId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ValidUserId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    [Fact]
    public void Create_with_client_identity_assigns_properties()
    {
        var participant = ChatParticipant.Create(
            ValidConversationId,
            ValidParticipantTypeId,
            clientId: ValidClientId);

        Assert.NotEqual(Guid.Empty, participant.Id);
        Assert.Equal(ValidConversationId, participant.ChatConversationId);
        Assert.Equal(ValidParticipantTypeId, participant.ParticipantTypeId);
        Assert.Equal(ValidClientId, participant.ClientId);
        Assert.Null(participant.AgentHumanId);
    }

    [Fact]
    public void Create_with_agent_human_identity_assigns_properties()
    {
        var participant = ChatParticipant.Create(
            ValidConversationId,
            ValidParticipantTypeId,
            agentHumanId: ValidAgentId);

        Assert.Equal(ValidAgentId, participant.AgentHumanId);
        Assert.Null(participant.ClientId);
    }

    [Fact]
    public void Create_with_empty_conversation_id_throws_argument_exception()
    {
        Assert.Throws<ArgumentException>(() =>
            ChatParticipant.Create(Guid.Empty, ValidParticipantTypeId, clientId: ValidClientId));
    }

    [Fact]
    public void Create_with_empty_participant_type_id_throws_argument_exception()
    {
        Assert.Throws<ArgumentException>(() =>
            ChatParticipant.Create(ValidConversationId, Guid.Empty, clientId: ValidClientId));
    }

    [Fact]
    public void Create_with_no_identity_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatParticipant.Create(ValidConversationId, ValidParticipantTypeId));

        Assert.Equal("clientId", exception.ParamName);
        Assert.Contains("exactamente una identidad", exception.Message);
    }

    [Fact]
    public void Create_with_client_and_agent_at_the_same_time_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatParticipant.Create(
                ValidConversationId,
                ValidParticipantTypeId,
                clientId: ValidClientId,
                agentHumanId: ValidAgentId));

        Assert.Equal("clientId", exception.ParamName);
    }

    [Fact]
    public void Create_with_empty_client_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatParticipant.Create(
                ValidConversationId,
                ValidParticipantTypeId,
                clientId: Guid.Empty));

        Assert.Equal("clientId", exception.ParamName);
    }

    [Fact]
    public void Create_with_empty_agent_human_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatParticipant.Create(
                ValidConversationId,
                ValidParticipantTypeId,
                agentHumanId: Guid.Empty));

        Assert.Equal("agentHumanId", exception.ParamName);
    }

    [Fact]
    public void ChangeIdentity_switches_to_agent_human()
    {
        var participant = ChatParticipant.Create(
            ValidConversationId,
            ValidParticipantTypeId,
            clientId: ValidClientId);

        participant.ChangeIdentity(agentHumanId: ValidAgentId);

        Assert.Null(participant.ClientId);
        Assert.Equal(ValidAgentId, participant.AgentHumanId);
        Assert.NotNull(participant.UpdatedAt);
    }

    [Fact]
    public void ChangeIdentity_switches_to_client()
    {
        var participant = ChatParticipant.Create(
            ValidConversationId,
            ValidParticipantTypeId,
            agentHumanId: ValidAgentId);

        participant.ChangeIdentity(clientId: ValidClientId);

        Assert.Equal(ValidClientId, participant.ClientId);
        Assert.Null(participant.AgentHumanId);
    }

    [Fact]
    public void ChangeIdentity_with_both_identities_throws_argument_exception()
    {
        var participant = ChatParticipant.Create(
            ValidConversationId,
            ValidParticipantTypeId,
            clientId: ValidClientId);

        var exception = Assert.Throws<ArgumentException>(() =>
            participant.ChangeIdentity(clientId: ValidClientId, agentHumanId: ValidAgentId));

        Assert.Equal("clientId", exception.ParamName);
        Assert.Equal(ValidClientId, participant.ClientId);
    }

    [Fact]
    public void ChangeIdentity_with_no_identity_throws_argument_exception()
    {
        var participant = ChatParticipant.Create(
            ValidConversationId,
            ValidParticipantTypeId,
            clientId: ValidClientId);

        var exception = Assert.Throws<ArgumentException>(() => participant.ChangeIdentity());

        Assert.Equal("clientId", exception.ParamName);
    }

    [Fact]
    public async Task Create_with_existing_client_persists_participant()
    {
        var context = new ChatParticipantTestContext();
        var conversation = ChatConversation.Create(context.Status.Id);
        var senderType = new SenderTypeEntity("Cliente");
        var client = TestClients.Create();
        context.Conversations[conversation.Id] = conversation;
        context.SenderTypes[senderType.Id] = senderType;
        context.Clients[client.Id] = client;

        var handler = new CreateChatParticipantCommandHandler(context.UnitOfWork);
        var participant = await handler.Handle(
            new CreateChatParticipantCommand(conversation.Id, senderType.Id, client.Id, null),
            CancellationToken.None);

        Assert.Contains(participant.Id, context.Participants.Keys);
        Assert.Equal(client.Id, participant.ClientId);
        Assert.Null(participant.AgentHumanId);
    }

    [Fact]
    public async Task Create_with_missing_client_throws_not_found()
    {
        var context = new ChatParticipantTestContext();
        var conversation = ChatConversation.Create(context.Status.Id);
        var senderType = new SenderTypeEntity("Cliente");
        context.Conversations[conversation.Id] = conversation;
        context.SenderTypes[senderType.Id] = senderType;
        var missingClientId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var handler = new CreateChatParticipantCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatParticipantCommand(conversation.Id, senderType.Id, missingClientId, null),
                CancellationToken.None));
        Assert.Empty(context.Participants);
    }

    [Fact]
    public async Task Create_with_missing_conversation_throws_not_found()
    {
        var context = new ChatParticipantTestContext();
        var senderType = new SenderTypeEntity("Usuario");
        context.SenderTypes[senderType.Id] = senderType;
        var missingConversationId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        var handler = new CreateChatParticipantCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatParticipantCommand(missingConversationId, senderType.Id, ValidClientId, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_with_missing_participant_type_throws_not_found()
    {
        var context = new ChatParticipantTestContext();
        var conversation = ChatConversation.Create(context.Status.Id);
        context.Conversations[conversation.Id] = conversation;
        var missingTypeId = Guid.Parse("88888888-8888-8888-8888-888888888888");

        var handler = new CreateChatParticipantCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatParticipantCommand(conversation.Id, missingTypeId, ValidClientId, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_with_existing_agent_persists_participant()
    {
        var context = new ChatParticipantTestContext();
        var conversation = ChatConversation.Create(context.Status.Id);
        var senderType = new SenderTypeEntity("Agente");
        var agent = AgentHuman.Create(ValidUserId);
        context.Conversations[conversation.Id] = conversation;
        context.SenderTypes[senderType.Id] = senderType;
        context.Agents[agent.Id] = agent;

        var handler = new CreateChatParticipantCommandHandler(context.UnitOfWork);
        var participant = await handler.Handle(
            new CreateChatParticipantCommand(conversation.Id, senderType.Id, null, agent.Id),
            CancellationToken.None);

        Assert.Equal(agent.Id, participant.AgentHumanId);
        Assert.Null(participant.ClientId);
    }

    [Fact]
    public async Task Create_with_missing_agent_throws_not_found()
    {
        var context = new ChatParticipantTestContext();
        var conversation = ChatConversation.Create(context.Status.Id);
        var senderType = new SenderTypeEntity("Agente");
        context.Conversations[conversation.Id] = conversation;
        context.SenderTypes[senderType.Id] = senderType;
        var missingAgentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var handler = new CreateChatParticipantCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatParticipantCommand(conversation.Id, senderType.Id, null, missingAgentId),
                CancellationToken.None));
    }

    [Fact]
    public async Task Change_identity_missing_participant_throws_not_found()
    {
        var context = new ChatParticipantTestContext();
        var handler = new ChangeChatParticipantIdentityCommandHandler(context.UnitOfWork);
        var missingParticipantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new ChangeChatParticipantIdentityCommand(missingParticipantId, ValidClientId, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Change_identity_with_existing_client_persists_change()
    {
        var context = new ChatParticipantTestContext();
        var client = TestClients.Create();
        var participant = ChatParticipant.Create(
            ValidConversationId,
            ValidParticipantTypeId,
            agentHumanId: ValidAgentId);
        context.Clients[client.Id] = client;
        context.Participants[participant.Id] = participant;

        var handler = new ChangeChatParticipantIdentityCommandHandler(context.UnitOfWork);
        var updated = await handler.Handle(
            new ChangeChatParticipantIdentityCommand(participant.Id, client.Id, null),
            CancellationToken.None);

        Assert.Equal(client.Id, updated.ClientId);
        Assert.Null(updated.AgentHumanId);
        Assert.Equal(client.Id, context.Participants[participant.Id].ClientId);
    }

    [Fact]
    public async Task Change_identity_with_missing_client_throws_not_found()
    {
        var context = new ChatParticipantTestContext();
        var participant = ChatParticipant.Create(
            ValidConversationId,
            ValidParticipantTypeId,
            agentHumanId: ValidAgentId);
        context.Participants[participant.Id] = participant;
        var missingClientId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        var handler = new ChangeChatParticipantIdentityCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new ChangeChatParticipantIdentityCommand(participant.Id, missingClientId, null),
                CancellationToken.None));
        Assert.Equal(ValidAgentId, context.Participants[participant.Id].AgentHumanId);
    }

    [Fact]
    public void Validators_require_exactly_one_identity()
    {
        var create = new CreateChatParticipantCommandValidator();
        var change = new ChangeChatParticipantIdentityCommandValidator();

        Assert.False(create.Validate(new CreateChatParticipantCommand(
            ValidConversationId, ValidParticipantTypeId, null, null)).IsValid);
        Assert.False(create.Validate(new CreateChatParticipantCommand(
            ValidConversationId, ValidParticipantTypeId, ValidClientId, ValidAgentId)).IsValid);
        Assert.True(create.Validate(new CreateChatParticipantCommand(
            ValidConversationId, ValidParticipantTypeId, ValidClientId, null)).IsValid);
        Assert.True(create.Validate(new CreateChatParticipantCommand(
            ValidConversationId, ValidParticipantTypeId, null, ValidAgentId)).IsValid);
        Assert.False(change.Validate(new ChangeChatParticipantIdentityCommand(
            Guid.NewGuid(), null, null)).IsValid);
        Assert.False(change.Validate(new ChangeChatParticipantIdentityCommand(
            Guid.NewGuid(), ValidClientId, ValidAgentId)).IsValid);
    }

    [Fact]
    public async Task Get_by_id_missing_participant_returns_null()
    {
        var context = new ChatParticipantTestContext();
        var handler = new GetChatParticipantByIdQueryHandler(context.UnitOfWork);
        var missingParticipantId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        var result = await handler.Handle(
            new GetChatParticipantByIdQuery(missingParticipantId),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Get_by_conversation_id_returns_repository_participants()
    {
        var context = new ChatParticipantTestContext();
        var conversation = ChatConversation.Create(context.Status.Id);
        var otherConversation = ChatConversation.Create(context.Status.Id);
        var senderType = new SenderTypeEntity("Usuario");
        var first = ChatParticipant.Create(
            conversation.Id,
            senderType.Id,
            clientId: ValidClientId);
        var second = ChatParticipant.Create(
            conversation.Id,
            senderType.Id,
            agentHumanId: ValidAgentId);
        var other = ChatParticipant.Create(
            otherConversation.Id,
            senderType.Id,
            clientId: ValidClientId);
        context.Participants[first.Id] = first;
        context.Participants[second.Id] = second;
        context.Participants[other.Id] = other;

        var handler = new GetChatParticipantsByConversationIdQueryHandler(context.UnitOfWork);
        var results = await handler.Handle(
            new GetChatParticipantsByConversationIdQuery(conversation.Id),
            CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, participant => participant.Id == first.Id);
        Assert.Contains(results, participant => participant.Id == second.Id);
        Assert.DoesNotContain(results, participant => participant.Id == other.Id);
    }

    private sealed class ChatParticipantTestContext
    {
        public Domain.ConversationStatuses.Entities.ConversationStatusEntity Status { get; } =
            new("Abierta");

        public Dictionary<Guid, ChatConversation> Conversations { get; } = new();
        public Dictionary<Guid, SenderTypeEntity> SenderTypes { get; } = new();
        public Dictionary<Guid, ClientEntity> Clients { get; } = new();
        public Dictionary<Guid, AgentHuman> Agents { get; } = new();
        public Dictionary<Guid, ChatParticipant> Participants { get; } = new();

        public IUnitOfWork UnitOfWork { get; }

        public ChatParticipantTestContext()
        {
            UnitOfWork = new FakeUnitOfWork(this);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly ChatParticipantTestContext _context;

        public FakeUnitOfWork(ChatParticipantTestContext context)
        {
            _context = context;
            ChatConversationsRepository = new FakeChatConversationRepository(context);
            SenderTypesRepository = new FakeSenderTypeRepository(context);
            ClientsRepository = CreateClientsRepository(context);
            AgentHumansRepository = new FakeAgentHumanRepository(context);
            ChatParticipantsRepository = new FakeChatParticipantRepository(context);
        }

        private static IClientRepository CreateClientsRepository(ChatParticipantTestContext context)
        {
            var repository = Substitute.For<IClientRepository>();
            repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult<ClientEntity?>(
                    context.Clients.TryGetValue(call.ArgAt<Guid>(0), out var client) ? client : null));
            return repository;
        }

        public IChatConversationRepository ChatConversationsRepository { get; }
        public ISenderTypeRepository SenderTypesRepository { get; }
        public IClientRepository ClientsRepository { get; }
        public IAgentHumanRepository AgentHumansRepository { get; }
        public IChatParticipantRepository ChatParticipantsRepository { get; }

        public IRolesRepository RolesRepository => null!;
        public Application.RolePermissions.Abstraction.IRolePermissionsRepository RolePermissionsRepository => null!;
        public Application.UserPermissions.Abstraction.IUserPermissionsRepository UserPermissionsRepository => null!;
        public Application.Modules.Abstraction.IModulesRepository ModulesRepository => null!;
        public ISpeciesRepository SpeciesRepository => null!;
        public IRaceRepository RacesRepository => null!;
        public IPetRepository PetsRepository => null!;
        public IUsersRepository UsersRepository => null!;
        public IStatusAppointmentRepository StatusAppointmentsRepository => null!;
        public ITypeServiceRepository TypeServicesRepository => null!;
        public IServiceRepository ServicesRepository => null!;
        public ISpecialtyRepository SpecialtiesRepository => null!;
        public IClientPetRepository ClientPetsRepository => null!;
        public IVeterinarianRepository VeterinariansRepository => null!;
        public IPriorityRepository PrioritiesRepository => null!;
        public IConversationStatusRepository ConversationStatusesRepository => null!;
        public IMessageTypeRepository MessageTypesRepository => null!;
        public IEscalationStatusRepository EscalationStatusesRepository => null!;
        public IAppointmentRepository AppointmentsRepository => null!;
        public IAppointmentStatusHistoryRepository AppointmentStatusHistoriesRepository => null!;
        public IMedicalRecordRepository MedicalRecordsRepository => null!;
        public IVaccinationRepository VaccinationsRepository => null!;
        public INotificationRepository NotificationsRepository => null!;
        public IDiagnosticRepository DiagnosticsRepository => null!;
        public IChatMessageRepository ChatMessagesRepository => null!;
        public IChatEscalationRepository ChatEscalationsRepository => null!;
        public IChatEscalationResolutionRepository ChatEscalationResolutionsRepository => null!;
        public IUserAccountsRepository UserAccountsRepository => null!;
        public IUserCredentialsRepository UserCredentialsRepository => null!;
        public IUserTokensRepository UserTokensRepository => null!;
        public IAccountStatementsRepository AccountStatementsRepository => null!;
        public IAvailabilityRepository AvailabilitiesRepository => null!;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken = default)
            => action(cancellationToken);
    }

    private sealed class FakeChatConversationRepository : IChatConversationRepository
    {
        private readonly ChatParticipantTestContext _context;

        public FakeChatConversationRepository(ChatParticipantTestContext context)
        {
            _context = context;
        }

        public Task<IReadOnlyCollection<ChatConversation>> GetAllAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ChatConversation>>(
                _context.Conversations.Values.ToArray());

        public Task<ChatConversation?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                _context.Conversations.TryGetValue(id, out var conversation) ? conversation : null);

        public Task AddAsync(ChatConversation conversation, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(ChatConversation conversation, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeSenderTypeRepository : ISenderTypeRepository
    {
        private readonly ChatParticipantTestContext _context;

        public FakeSenderTypeRepository(ChatParticipantTestContext context)
        {
            _context = context;
        }

        public Task<IReadOnlyCollection<SenderTypeEntity>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<SenderTypeEntity>>(
                _context.SenderTypes.Values.ToArray());

        public Task<SenderTypeEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(
                _context.SenderTypes.TryGetValue(id, out var senderType) ? senderType : null);

        public Task AddAsync(SenderTypeEntity senderType, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task UpdateAsync(SenderTypeEntity senderType, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task DeleteAsync(SenderTypeEntity senderType, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeAgentHumanRepository : IAgentHumanRepository
    {
        private readonly ChatParticipantTestContext _context;

        public FakeAgentHumanRepository(ChatParticipantTestContext context)
        {
            _context = context;
        }

        public Task<IReadOnlyCollection<AgentHuman>> GetAllAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<AgentHuman>>(
                _context.Agents.Values.ToArray());

        public Task<AgentHuman?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                _context.Agents.TryGetValue(id, out var agent) ? agent : null);

        public Task<IReadOnlyCollection<AgentHuman>> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<AgentHuman>>(
                _context.Agents.Values
                    .Where(agent => agent.UserId == userId)
                    .ToArray());

        public Task AddAsync(AgentHuman agent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(AgentHuman agent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeChatParticipantRepository : IChatParticipantRepository
    {
        private readonly ChatParticipantTestContext _context;

        public FakeChatParticipantRepository(ChatParticipantTestContext context)
        {
            _context = context;
        }

        public Task AddAsync(ChatParticipant participant, CancellationToken cancellationToken = default)
        {
            _context.Participants[participant.Id] = participant;
            return Task.CompletedTask;
        }

        public Task<ChatParticipant?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                _context.Participants.TryGetValue(id, out var participant) ? participant : null);

        public Task<IReadOnlyCollection<ChatParticipant>> GetAllByConversationIdAsync(
            Guid chatConversationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ChatParticipant>>(
                _context.Participants.Values
                    .Where(participant => participant.ChatConversationId == chatConversationId)
                    .ToArray());

        public Task UpdateAsync(ChatParticipant participant, CancellationToken cancellationToken = default)
        {
            _context.Participants[participant.Id] = participant;
            return Task.CompletedTask;
        }
    }
}
