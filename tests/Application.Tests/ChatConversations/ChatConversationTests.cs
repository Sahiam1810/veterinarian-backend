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
using Application.ChatConversations.UseCase;
using Domain.ChatConversations.Entities;
using Domain.ConversationStatuses.Entities;
using Domain.Priorities.Entities;
using Xunit;

namespace Application.Tests.ChatConversations;

public sealed class ChatConversationTests
{
    private static readonly Guid ValidStatusId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ValidPriorityId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ValidClosedById = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void Create_with_valid_status_is_open_with_ai_enabled_and_no_priority()
    {
        var conversation = ChatConversation.Create(ValidStatusId);

        Assert.Equal(ValidStatusId, conversation.ConversationStatusId);
        Assert.Null(conversation.PriorityId);
        Assert.True(conversation.AiEnabled);
        Assert.False(conversation.Closed);
        Assert.Null(conversation.ClosedAt);
        Assert.Null(conversation.ClosedBy);
        Assert.NotEqual(Guid.Empty, conversation.Id);
    }

    [Fact]
    public void Create_with_empty_status_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ChatConversation.Create(Guid.Empty));

        Assert.Equal("conversationStatusId", exception.ParamName);
    }

    [Fact]
    public void Close_sets_closed_state_and_closed_by_when_provided()
    {
        var conversation = ChatConversation.Create(ValidStatusId);

        conversation.Close(ValidClosedById);

        Assert.True(conversation.Closed);
        Assert.NotNull(conversation.ClosedAt);
        Assert.Equal(ValidClosedById, conversation.ClosedBy);
    }

    [Fact]
    public void Close_with_empty_closed_by_throws_argument_exception()
    {
        var conversation = ChatConversation.Create(ValidStatusId);

        var exception = Assert.Throws<ArgumentException>(
            () => conversation.Close(Guid.Empty));

        Assert.Equal("closedBy", exception.ParamName);
    }

    [Fact]
    public void Reopen_clears_closed_state()
    {
        var conversation = ChatConversation.Create(ValidStatusId);
        conversation.Close(ValidClosedById);

        conversation.Reopen();

        Assert.False(conversation.Closed);
        Assert.Null(conversation.ClosedAt);
        Assert.Null(conversation.ClosedBy);
    }

    [Fact]
    public void Change_status_priority_and_ai_enabled_update_state()
    {
        var conversation = ChatConversation.Create(ValidStatusId);
        var newStatusId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        conversation.ChangeStatus(newStatusId);
        conversation.SetPriority(ValidPriorityId);
        conversation.SetAiEnabled(false);
        conversation.UpdateLastMessageAt(DateTime.UtcNow);

        Assert.Equal(newStatusId, conversation.ConversationStatusId);
        Assert.Equal(ValidPriorityId, conversation.PriorityId);
        Assert.False(conversation.AiEnabled);
        Assert.NotNull(conversation.LastMessageAt);
    }

    [Fact]
    public async Task Create_with_existing_status_persists_conversation()
    {
        var context = new ChatConversationTestContext();
        var status = new ConversationStatusEntity("Abierta");
        context.Statuses[status.Id] = status;

        var handler = new CreateChatConversationCommandHandler(context.UnitOfWork);
        var conversation = await handler.Handle(
            new CreateChatConversationCommand(status.Id, null),
            CancellationToken.None);

        Assert.Contains(conversation.Id, context.Conversations.Keys);
        Assert.Equal(status.Id, conversation.ConversationStatusId);
    }

    [Fact]
    public async Task Create_with_missing_status_throws_not_found()
    {
        var context = new ChatConversationTestContext();
        var handler = new CreateChatConversationCommandHandler(context.UnitOfWork);
        var missingStatusId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatConversationCommand(missingStatusId, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_with_existing_priority_assigns_priority()
    {
        var context = new ChatConversationTestContext();
        var status = new ConversationStatusEntity("Abierta");
        var priority = new PriorityEntity("Alta");
        context.Statuses[status.Id] = status;
        context.Priorities[priority.Id] = priority;

        var handler = new CreateChatConversationCommandHandler(context.UnitOfWork);
        var conversation = await handler.Handle(
            new CreateChatConversationCommand(status.Id, priority.Id),
            CancellationToken.None);

        Assert.Equal(priority.Id, conversation.PriorityId);
    }

    [Fact]
    public async Task Create_with_missing_priority_throws_not_found()
    {
        var context = new ChatConversationTestContext();
        var status = new ConversationStatusEntity("Abierta");
        context.Statuses[status.Id] = status;
        var missingPriorityId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        var handler = new CreateChatConversationCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatConversationCommand(status.Id, missingPriorityId),
                CancellationToken.None));
    }

    [Fact]
    public async Task Close_missing_conversation_throws_not_found()
    {
        var context = new ChatConversationTestContext();
        var handler = new CloseChatConversationCommandHandler(context.UnitOfWork);
        var missingConversationId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CloseChatConversationCommand(missingConversationId),
                CancellationToken.None));
    }

    [Fact]
    public async Task Reopen_applies_domain_operation()
    {
        var context = new ChatConversationTestContext();
        var status = new ConversationStatusEntity("Cerrada");
        var conversation = ChatConversation.Create(status.Id);
        conversation.Close(ValidClosedById);
        context.Conversations[conversation.Id] = conversation;

        var handler = new ReopenChatConversationCommandHandler(context.UnitOfWork);
        var reopened = await handler.Handle(
            new ReopenChatConversationCommand(conversation.Id),
            CancellationToken.None);

        Assert.False(reopened.Closed);
        Assert.Null(reopened.ClosedAt);
        Assert.Null(reopened.ClosedBy);
    }

    [Fact]
    public void Create_command_with_empty_status_fails_validation()
    {
        var validator = new CreateChatConversationCommandValidator();

        var result = validator.Validate(new CreateChatConversationCommand(Guid.Empty, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Close_command_with_empty_closed_by_fails_validation()
    {
        var validator = new CloseChatConversationCommandValidator();

        var result = validator.Validate(new CloseChatConversationCommand(
            Guid.Parse("88888888-8888-8888-8888-888888888888"),
            Guid.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Create_command_with_empty_priority_fails_validation()
    {
        var validator = new CreateChatConversationCommandValidator();

        var result = validator.Validate(
            new CreateChatConversationCommand(ValidStatusId, Guid.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Update_priority_command_with_empty_priority_fails_validation()
    {
        var validator = new UpdateChatConversationPriorityCommandValidator();

        var result = validator.Validate(
            new UpdateChatConversationPriorityCommand(
                Guid.Parse("99999999-9999-9999-9999-999999999999"),
                Guid.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Close_when_already_closed_preserves_closed_at_and_closed_by()
    {
        var conversation = ChatConversation.Create(ValidStatusId);
        conversation.Close(ValidClosedById);
        var originalClosedAt = conversation.ClosedAt;
        var originalClosedBy = conversation.ClosedBy;
        var originalUpdatedAt = conversation.UpdatedAt;

        conversation.Close(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        Assert.True(conversation.Closed);
        Assert.Equal(originalClosedAt, conversation.ClosedAt);
        Assert.Equal(originalClosedBy, conversation.ClosedBy);
        Assert.Equal(originalUpdatedAt, conversation.UpdatedAt);
    }

    [Fact]
    public async Task Update_status_missing_conversation_throws_not_found()
    {
        var context = new ChatConversationTestContext();
        var handler = new UpdateChatConversationStatusCommandHandler(context.UnitOfWork);
        var missingConversationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var status = new ConversationStatusEntity("Abierta");
        context.Statuses[status.Id] = status;

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new UpdateChatConversationStatusCommand(missingConversationId, status.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task Update_status_missing_catalog_status_throws_not_found()
    {
        var context = new ChatConversationTestContext();
        var status = new ConversationStatusEntity("Abierta");
        var conversation = ChatConversation.Create(status.Id);
        context.Conversations[conversation.Id] = conversation;
        var missingStatusId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var handler = new UpdateChatConversationStatusCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new UpdateChatConversationStatusCommand(conversation.Id, missingStatusId),
                CancellationToken.None));
    }

    [Fact]
    public async Task Update_status_with_valid_status_persists_change()
    {
        var context = new ChatConversationTestContext();
        var currentStatus = new ConversationStatusEntity("Abierta");
        var newStatus = new ConversationStatusEntity("En progreso");
        context.Statuses[currentStatus.Id] = currentStatus;
        context.Statuses[newStatus.Id] = newStatus;
        var conversation = ChatConversation.Create(currentStatus.Id);
        context.Conversations[conversation.Id] = conversation;

        var handler = new UpdateChatConversationStatusCommandHandler(context.UnitOfWork);
        var updated = await handler.Handle(
            new UpdateChatConversationStatusCommand(conversation.Id, newStatus.Id),
            CancellationToken.None);

        Assert.Equal(newStatus.Id, updated.ConversationStatusId);
        Assert.Equal(newStatus.Id, context.Conversations[conversation.Id].ConversationStatusId);
    }

    [Fact]
    public async Task Update_priority_missing_conversation_throws_not_found()
    {
        var context = new ChatConversationTestContext();
        var handler = new UpdateChatConversationPriorityCommandHandler(context.UnitOfWork);
        var missingConversationId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new UpdateChatConversationPriorityCommand(missingConversationId, ValidPriorityId),
                CancellationToken.None));
    }

    [Fact]
    public async Task Update_priority_missing_catalog_priority_throws_not_found()
    {
        var context = new ChatConversationTestContext();
        var status = new ConversationStatusEntity("Abierta");
        var conversation = ChatConversation.Create(status.Id);
        context.Conversations[conversation.Id] = conversation;
        var missingPriorityId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        var handler = new UpdateChatConversationPriorityCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new UpdateChatConversationPriorityCommand(conversation.Id, missingPriorityId),
                CancellationToken.None));
    }

    [Fact]
    public async Task Update_priority_with_existing_priority_persists_change()
    {
        var context = new ChatConversationTestContext();
        var status = new ConversationStatusEntity("Abierta");
        var priority = new PriorityEntity("Alta");
        context.Statuses[status.Id] = status;
        context.Priorities[priority.Id] = priority;
        var conversation = ChatConversation.Create(status.Id);
        context.Conversations[conversation.Id] = conversation;

        var handler = new UpdateChatConversationPriorityCommandHandler(context.UnitOfWork);
        var updated = await handler.Handle(
            new UpdateChatConversationPriorityCommand(conversation.Id, priority.Id),
            CancellationToken.None);

        Assert.Equal(priority.Id, updated.PriorityId);
        Assert.Equal(priority.Id, context.Conversations[conversation.Id].PriorityId);
    }

    [Fact]
    public async Task Update_priority_with_null_removes_priority()
    {
        var context = new ChatConversationTestContext();
        var status = new ConversationStatusEntity("Abierta");
        var priority = new PriorityEntity("Alta");
        context.Statuses[status.Id] = status;
        context.Priorities[priority.Id] = priority;
        var conversation = ChatConversation.Create(status.Id, priority.Id);
        context.Conversations[conversation.Id] = conversation;

        var handler = new UpdateChatConversationPriorityCommandHandler(context.UnitOfWork);
        var updated = await handler.Handle(
            new UpdateChatConversationPriorityCommand(conversation.Id, null),
            CancellationToken.None);

        Assert.Null(updated.PriorityId);
        Assert.Null(context.Conversations[conversation.Id].PriorityId);
    }

    [Fact]
    public async Task Update_ai_enabled_missing_conversation_throws_not_found()
    {
        var context = new ChatConversationTestContext();
        var handler = new UpdateChatConversationAiEnabledCommandHandler(context.UnitOfWork);
        var missingConversationId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new UpdateChatConversationAiEnabledCommand(missingConversationId, false),
                CancellationToken.None));
    }

    [Fact]
    public async Task Update_ai_enabled_with_valid_value_persists_change()
    {
        var context = new ChatConversationTestContext();
        var status = new ConversationStatusEntity("Abierta");
        var conversation = ChatConversation.Create(status.Id);
        context.Conversations[conversation.Id] = conversation;

        var handler = new UpdateChatConversationAiEnabledCommandHandler(context.UnitOfWork);
        var updated = await handler.Handle(
            new UpdateChatConversationAiEnabledCommand(conversation.Id, false),
            CancellationToken.None);

        Assert.False(updated.AiEnabled);
        Assert.False(context.Conversations[conversation.Id].AiEnabled);
    }

    [Fact]
    public async Task Get_by_id_missing_conversation_returns_null()
    {
        var context = new ChatConversationTestContext();
        var handler = new GetChatConversationByIdQueryHandler(context.UnitOfWork);
        var missingConversationId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        var result = await handler.Handle(
            new GetChatConversationByIdQuery(missingConversationId),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Get_all_returns_repository_conversations()
    {
        var context = new ChatConversationTestContext();
        var status = new ConversationStatusEntity("Abierta");
        var first = ChatConversation.Create(status.Id);
        var second = ChatConversation.Create(status.Id);
        context.Conversations[first.Id] = first;
        context.Conversations[second.Id] = second;

        var handler = new GetAllChatConversationsQueryHandler(context.UnitOfWork);
        var results = await handler.Handle(new GetAllChatConversationsQuery(), CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, conversation => conversation.Id == first.Id);
        Assert.Contains(results, conversation => conversation.Id == second.Id);
    }

    [Fact]
    public async Task Reopen_missing_conversation_throws_not_found()
    {
        var context = new ChatConversationTestContext();
        var handler = new ReopenChatConversationCommandHandler(context.UnitOfWork);
        var missingConversationId = Guid.Parse("12121212-1212-1212-1212-121212121212");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new ReopenChatConversationCommand(missingConversationId),
                CancellationToken.None));
    }

    private sealed class ChatConversationTestContext
    {
        public Dictionary<Guid, ConversationStatusEntity> Statuses { get; } = new();
        public Dictionary<Guid, PriorityEntity> Priorities { get; } = new();
        public Dictionary<Guid, ChatConversation> Conversations { get; } = new();

        public IUnitOfWork UnitOfWork { get; }

        public ChatConversationTestContext()
        {
            UnitOfWork = new FakeUnitOfWork(this);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly ChatConversationTestContext _context;

        public FakeUnitOfWork(ChatConversationTestContext context)
        {
            _context = context;
            ConversationStatusesRepository = new FakeConversationStatusRepository(context);
            PrioritiesRepository = new FakePriorityRepository(context);
            ChatConversationsRepository = new FakeChatConversationRepository(context);
        }

        public IConversationStatusRepository ConversationStatusesRepository { get; }
        public IPriorityRepository PrioritiesRepository { get; }
        public IChatConversationRepository ChatConversationsRepository { get; }
        public IChatParticipantRepository ChatParticipantsRepository => null!;
        public IChatMessageRepository ChatMessagesRepository => null!;
        public IChatAttachmentRepository ChatAttachmentsRepository => null!;
        public IChatAiRunRepository ChatAiRunsRepository => null!;
        public IChatAiRunMetricsRepository ChatAiRunMetricsRepository => null!;
        public IChatAiRunErrorRepository ChatAiRunErrorsRepository => null!;

        public IRolesRepository RolesRepository => null!;
        public Application.RolePermissions.Abstraction.IRolePermissionsRepository RolePermissionsRepository => null!;
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
        public ISenderTypeRepository SenderTypesRepository => null!;
        public IAiRunStatusRepository AiRunStatusesRepository => null!;
        public IMessageTypeRepository MessageTypesRepository => null!;
        public IEscalationStatusRepository EscalationStatusesRepository => null!;
        public IAppointmentRepository AppointmentsRepository => null!;
        public IAppointmentStatusHistoryRepository AppointmentStatusHistoriesRepository => null!;
        public IMedicalRecordRepository MedicalRecordsRepository => null!;
        public IVaccinationRepository VaccinationsRepository => null!;
        public INotificationRepository NotificationsRepository => null!;
        public IDiagnosticRepository DiagnosticsRepository => null!;
        public IAgentHumanRepository AgentHumansRepository => null!;
        public IAiModelRepository AiModelsRepository => null!;
        public IChatUserProfileRepository ChatUserProfilesRepository => null!;
        public IChatConversationAssignmentRepository ChatConversationAssignmentsRepository => null!;
        public IChatConversationAiSettingRepository ChatConversationAiSettingsRepository => null!;
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

    private sealed class FakeConversationStatusRepository : IConversationStatusRepository
    {
        private readonly ChatConversationTestContext _context;

        public FakeConversationStatusRepository(ChatConversationTestContext context)
        {
            _context = context;
        }

        public Task<IReadOnlyCollection<ConversationStatusEntity>> GetAllAsync(
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ConversationStatusEntity>>(
                _context.Statuses.Values.ToArray());

        public Task<ConversationStatusEntity?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
            => Task.FromResult(
                _context.Statuses.TryGetValue(id, out var status) ? status : null);

        public Task AddAsync(
            ConversationStatusEntity conversationStatus,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task UpdateAsync(
            ConversationStatusEntity conversationStatus,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task DeleteAsync(
            ConversationStatusEntity conversationStatus,
            CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakePriorityRepository : IPriorityRepository
    {
        private readonly ChatConversationTestContext _context;

        public FakePriorityRepository(ChatConversationTestContext context)
        {
            _context = context;
        }

        public Task<IReadOnlyCollection<PriorityEntity>> GetAllAsync(
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<PriorityEntity>>(
                _context.Priorities.Values.ToArray());

        public Task<PriorityEntity?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
            => Task.FromResult(
                _context.Priorities.TryGetValue(id, out var priority) ? priority : null);

        public Task AddAsync(PriorityEntity priority, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task UpdateAsync(PriorityEntity priority, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task DeleteAsync(PriorityEntity priority, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeChatConversationRepository : IChatConversationRepository
    {
        private readonly ChatConversationTestContext _context;

        public FakeChatConversationRepository(ChatConversationTestContext context)
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

        public Task AddAsync(
            ChatConversation conversation,
            CancellationToken cancellationToken = default)
        {
            _context.Conversations[conversation.Id] = conversation;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            ChatConversation conversation,
            CancellationToken cancellationToken = default)
        {
            _context.Conversations[conversation.Id] = conversation;
            return Task.CompletedTask;
        }
    }
}
