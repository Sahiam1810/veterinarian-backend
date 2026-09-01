using Application.AccountStatements.Abstraction;
using Application.AgentHumans.Abstraction;
using Application.AiModels.Abstraction;
using Application.AiRunStatuses.Abstraction;
using Application.Appointments.Abstraction;
using Application.AppointmentStatusHistories.Abstraction;
using Application.Availabilities.Abstraction;
using Application.ChatConversationAssignments.Abstraction;
using Application.ChatConversationAiSettings.Abstraction;
using Application.ChatAiRunErrors.Abstraction;
using Application.ChatAiRunMetrics.Abstraction;
using Application.ChatAiRuns.Abstraction;
using Application.ChatConversations.Abstraction;
using Application.ChatAttachments.Abstraction;
using Application.ChatEscalationAssignments.Abstraction;
using Application.ChatEscalationResolutions.Abstraction;
using Application.ChatEscalations.Abstraction;
using Application.ChatEscalationStatusHistories.Abstraction;
using Application.ChatMessages.Abstraction;
using Application.ChatParticipants.Abstraction;
using Application.ChatParticipants.UseCase;
using Application.ChatUserProfiles.Abstraction;
using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.ConversationStatuses.Abstraction;
using Application.Diagnostics.Abstraction;
using Application.EscalationStatuses.Abstraction;
using Application.MedicalRecords.Abstraction;
using Application.MessageTypes.Abstraction;
using Application.Notifications.Abstraction;
using Application.Pets.Abstraction;
using Application.Priorities.Abstraction;
using Application.ProviderModelsAi.Abstraction;
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
using Domain.ChatUserProfiles.Entities;
using Domain.SenderTypes.Entities;
using Xunit;

namespace Application.Tests.ChatParticipants;

public sealed class ChatParticipantTests
{
    private static readonly Guid ValidConversationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ValidParticipantTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ValidProfileId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ValidAgentId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ValidAiModelId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid ValidUserId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    [Fact]
    public void Create_with_valid_profile_identity_assigns_properties()
    {
        var participant = ChatParticipant.Create(
            ValidConversationId,
            ValidParticipantTypeId,
            chatUserProfileId: ValidProfileId);

        Assert.Equal(ValidConversationId, participant.ChatConversationId);
        Assert.Equal(ValidParticipantTypeId, participant.ParticipantTypeId);
        Assert.Equal(ValidProfileId, participant.ChatUserProfileId);
        Assert.Null(participant.AgentHumanId);
        Assert.Null(participant.AiModelId);
        Assert.NotEqual(Guid.Empty, participant.Id);
    }

    [Fact]
    public void Create_with_empty_conversation_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatParticipant.Create(Guid.Empty, ValidParticipantTypeId, chatUserProfileId: ValidProfileId));

        Assert.Equal("chatConversationId", exception.ParamName);
    }

    [Fact]
    public void Create_with_empty_participant_type_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatParticipant.Create(ValidConversationId, Guid.Empty, chatUserProfileId: ValidProfileId));

        Assert.Equal("participantTypeId", exception.ParamName);
    }

    [Fact]
    public void Create_with_no_identity_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatParticipant.Create(ValidConversationId, ValidParticipantTypeId));

        Assert.Equal("chatUserProfileId", exception.ParamName);
    }

    [Fact]
    public void Create_with_multiple_identities_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatParticipant.Create(
                ValidConversationId,
                ValidParticipantTypeId,
                chatUserProfileId: ValidProfileId,
                agentHumanId: ValidAgentId));

        Assert.Equal("chatUserProfileId", exception.ParamName);
    }

    [Fact]
    public void Create_with_empty_chat_user_profile_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatParticipant.Create(
                ValidConversationId,
                ValidParticipantTypeId,
                chatUserProfileId: Guid.Empty));

        Assert.Equal("chatUserProfileId", exception.ParamName);
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
    public void Create_with_empty_ai_model_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatParticipant.Create(
                ValidConversationId,
                ValidParticipantTypeId,
                aiModelId: Guid.Empty));

        Assert.Equal("aiModelId", exception.ParamName);
    }

    [Fact]
    public void ChangeIdentity_switches_to_agent_human()
    {
        var participant = ChatParticipant.Create(
            ValidConversationId,
            ValidParticipantTypeId,
            chatUserProfileId: ValidProfileId);

        participant.ChangeIdentity(agentHumanId: ValidAgentId);

        Assert.Null(participant.ChatUserProfileId);
        Assert.Equal(ValidAgentId, participant.AgentHumanId);
        Assert.Null(participant.AiModelId);
        Assert.NotNull(participant.UpdatedAt);
    }

    [Fact]
    public void ChangeIdentity_with_multiple_identities_throws_argument_exception()
    {
        var participant = ChatParticipant.Create(
            ValidConversationId,
            ValidParticipantTypeId,
            chatUserProfileId: ValidProfileId);

        var exception = Assert.Throws<ArgumentException>(() =>
            participant.ChangeIdentity(
                chatUserProfileId: ValidProfileId,
                agentHumanId: ValidAgentId));

        Assert.Equal("chatUserProfileId", exception.ParamName);
    }

    [Fact]
    public void ChangeIdentity_with_no_identity_throws_argument_exception()
    {
        var participant = ChatParticipant.Create(
            ValidConversationId,
            ValidParticipantTypeId,
            chatUserProfileId: ValidProfileId);

        var exception = Assert.Throws<ArgumentException>(() =>
            participant.ChangeIdentity());

        Assert.Equal("chatUserProfileId", exception.ParamName);
    }

    [Fact]
    public async Task Create_with_existing_conversation_and_type_persists_participant()
    {
        var context = new ChatParticipantTestContext();
        var conversation = ChatConversation.Create(context.Status.Id);
        var senderType = new SenderTypeEntity("Usuario");
        context.Conversations[conversation.Id] = conversation;
        context.SenderTypes[senderType.Id] = senderType;

        var handler = new CreateChatParticipantCommandHandler(context.UnitOfWork);
        var participant = await handler.Handle(
            new CreateChatParticipantCommand(
                conversation.Id,
                senderType.Id,
                null,
                null,
                ValidAiModelId),
            CancellationToken.None);

        Assert.Contains(participant.Id, context.Participants.Keys);
        Assert.Equal(conversation.Id, participant.ChatConversationId);
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
                new CreateChatParticipantCommand(
                    missingConversationId,
                    senderType.Id,
                    ValidProfileId,
                    null,
                    null),
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
                new CreateChatParticipantCommand(
                    conversation.Id,
                    missingTypeId,
                    ValidProfileId,
                    null,
                    null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_with_existing_profile_persists_participant()
    {
        var context = new ChatParticipantTestContext();
        var conversation = ChatConversation.Create(context.Status.Id);
        var senderType = new SenderTypeEntity("Usuario");
        var profile = ChatUserProfile.Create(ValidUserId, "Perfil", null, null);
        context.Conversations[conversation.Id] = conversation;
        context.SenderTypes[senderType.Id] = senderType;
        context.Profiles[profile.Id] = profile;

        var handler = new CreateChatParticipantCommandHandler(context.UnitOfWork);
        var participant = await handler.Handle(
            new CreateChatParticipantCommand(
                conversation.Id,
                senderType.Id,
                profile.Id,
                null,
                null),
            CancellationToken.None);

        Assert.Equal(profile.Id, participant.ChatUserProfileId);
    }

    [Fact]
    public async Task Create_with_missing_profile_throws_not_found()
    {
        var context = new ChatParticipantTestContext();
        var conversation = ChatConversation.Create(context.Status.Id);
        var senderType = new SenderTypeEntity("Usuario");
        context.Conversations[conversation.Id] = conversation;
        context.SenderTypes[senderType.Id] = senderType;
        var missingProfileId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var handler = new CreateChatParticipantCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatParticipantCommand(
                    conversation.Id,
                    senderType.Id,
                    missingProfileId,
                    null,
                    null),
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
            new CreateChatParticipantCommand(
                conversation.Id,
                senderType.Id,
                null,
                agent.Id,
                null),
            CancellationToken.None);

        Assert.Equal(agent.Id, participant.AgentHumanId);
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
                new CreateChatParticipantCommand(
                    conversation.Id,
                    senderType.Id,
                    null,
                    missingAgentId,
                    null),
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
                new ChangeChatParticipantIdentityCommand(
                    missingParticipantId,
                    ValidProfileId,
                    null,
                    null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Change_identity_with_existing_profile_persists_change()
    {
        var context = new ChatParticipantTestContext();
        var conversation = ChatConversation.Create(context.Status.Id);
        var senderType = new SenderTypeEntity("Usuario");
        var profile = ChatUserProfile.Create(ValidUserId, "Perfil", null, null);
        var participant = ChatParticipant.Create(
            conversation.Id,
            senderType.Id,
            aiModelId: ValidAiModelId);
        context.Conversations[conversation.Id] = conversation;
        context.SenderTypes[senderType.Id] = senderType;
        context.Profiles[profile.Id] = profile;
        context.Participants[participant.Id] = participant;

        var handler = new ChangeChatParticipantIdentityCommandHandler(context.UnitOfWork);
        var updated = await handler.Handle(
            new ChangeChatParticipantIdentityCommand(
                participant.Id,
                profile.Id,
                null,
                null),
            CancellationToken.None);

        Assert.Equal(profile.Id, updated.ChatUserProfileId);
        Assert.Null(updated.AiModelId);
        Assert.Equal(profile.Id, context.Participants[participant.Id].ChatUserProfileId);
    }

    [Fact]
    public async Task Change_identity_with_missing_profile_throws_not_found()
    {
        var context = new ChatParticipantTestContext();
        var conversation = ChatConversation.Create(context.Status.Id);
        var senderType = new SenderTypeEntity("Usuario");
        var participant = ChatParticipant.Create(
            conversation.Id,
            senderType.Id,
            aiModelId: ValidAiModelId);
        context.Participants[participant.Id] = participant;
        var missingProfileId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        var handler = new ChangeChatParticipantIdentityCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new ChangeChatParticipantIdentityCommand(
                    participant.Id,
                    missingProfileId,
                    null,
                    null),
                CancellationToken.None));
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
            chatUserProfileId: ValidProfileId);
        var second = ChatParticipant.Create(
            conversation.Id,
            senderType.Id,
            aiModelId: ValidAiModelId);
        var other = ChatParticipant.Create(
            otherConversation.Id,
            senderType.Id,
            chatUserProfileId: ValidProfileId);
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
        public Dictionary<Guid, ChatUserProfile> Profiles { get; } = new();
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
            ChatUserProfilesRepository = new FakeChatUserProfileRepository(context);
            AgentHumansRepository = new FakeAgentHumanRepository(context);
            ChatParticipantsRepository = new FakeChatParticipantRepository(context);
        }

        public IChatConversationRepository ChatConversationsRepository { get; }
        public ISenderTypeRepository SenderTypesRepository { get; }
        public IChatUserProfileRepository ChatUserProfilesRepository { get; }
        public IAgentHumanRepository AgentHumansRepository { get; }
        public IChatParticipantRepository ChatParticipantsRepository { get; }
        public IChatAiRunRepository ChatAiRunsRepository => null!;
        public IChatAiRunMetricsRepository ChatAiRunMetricsRepository => null!;
        public IChatAiRunErrorRepository ChatAiRunErrorsRepository => null!;

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
        public IAiRunStatusRepository AiRunStatusesRepository => null!;
        public IConversationStatusRepository ConversationStatusesRepository => null!;
        public IMessageTypeRepository MessageTypesRepository => null!;
        public IEscalationStatusRepository EscalationStatusesRepository => null!;
        public IAppointmentRepository AppointmentsRepository => null!;
        public IAppointmentStatusHistoryRepository AppointmentStatusHistoriesRepository => null!;
        public IMedicalRecordRepository MedicalRecordsRepository => null!;
        public IVaccinationRepository VaccinationsRepository => null!;
        public INotificationRepository NotificationsRepository => null!;
        public IDiagnosticRepository DiagnosticsRepository => null!;
        public IAiModelRepository AiModelsRepository => null!;
        public IChatConversationAssignmentRepository ChatConversationAssignmentsRepository => null!;
        public IChatConversationAiSettingRepository ChatConversationAiSettingsRepository => null!;
        public IChatMessageRepository ChatMessagesRepository => null!;
        public IChatAttachmentRepository ChatAttachmentsRepository => null!;
        public IChatEscalationRepository ChatEscalationsRepository => null!;
        public IChatEscalationStatusHistoryRepository ChatEscalationStatusHistoriesRepository => null!;
        public IChatEscalationResolutionRepository ChatEscalationResolutionsRepository => null!;
        public IChatEscalationAssignmentRepository ChatEscalationAssignmentsRepository => null!;
        public IProviderModelAiRepository ProviderModelsAiRepository => null!;
        public IUserAccountsRepository UserAccountsRepository => null!;
        public IUserCredentialsRepository UserCredentialsRepository => null!;
        public IClientRepository ClientsRepository => null!;
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

    private sealed class FakeChatUserProfileRepository : IChatUserProfileRepository
    {
        private readonly ChatParticipantTestContext _context;

        public FakeChatUserProfileRepository(ChatParticipantTestContext context)
        {
            _context = context;
        }

        public Task<IReadOnlyCollection<ChatUserProfile>> GetAllAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ChatUserProfile>>(
                _context.Profiles.Values.ToArray());

        public Task<ChatUserProfile?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                _context.Profiles.TryGetValue(id, out var profile) ? profile : null);

        public Task<IReadOnlyCollection<ChatUserProfile>> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ChatUserProfile>>(
                _context.Profiles.Values
                    .Where(profile => profile.UserId == userId)
                    .ToArray());

        public Task AddAsync(ChatUserProfile profile, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(ChatUserProfile profile, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(ChatUserProfile profile, CancellationToken cancellationToken = default)
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
