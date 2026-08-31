using Application.AiModels.Abstraction;
using Application.AiRunStatuses.Abstraction;
using Application.ChatAiRunErrors.Abstraction;
using Application.ChatAiRunMetrics.Abstraction;
using Application.ChatAiRuns.Abstraction;
using Application.ChatAiRuns.UseCase;
using Application.ChatAttachments.Abstraction;
using Application.ChatConversations.Abstraction;
using Application.ChatMessages.Abstraction;
using Application.ChatParticipants.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.AiModels.Entities;
using Domain.AiRunStatuses.Entities;
using Domain.ChatConversations.Entities;
using Domain.ChatMessages.Entities;
using Domain.ChatAiRuns.Entities;
using Xunit;

namespace Application.Tests.ChatAiRuns;

public sealed class ChatAiRunTests
{
    private static readonly Guid ValidConversationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ValidMessageId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ValidAiModelId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ValidAiRunStatusId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ValidOtherStatusId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    [Fact]
    public void Create_with_valid_data_assigns_properties()
    {
        var run = ChatAiRun.Create(
            ValidConversationId,
            ValidMessageId,
            ValidAiModelId,
            ValidAiRunStatusId);

        Assert.Equal(ValidConversationId, run.ChatConversationId);
        Assert.Equal(ValidMessageId, run.ChatMessageId);
        Assert.Equal(ValidAiModelId, run.AiModelId);
        Assert.Equal(ValidAiRunStatusId, run.AiRunStatusId);
        Assert.NotEqual(Guid.Empty, run.Id);
    }

    [Fact]
    public void Create_with_empty_conversation_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAiRun.Create(Guid.Empty, ValidMessageId, ValidAiModelId, ValidAiRunStatusId));

        Assert.Equal("chatConversationId", exception.ParamName);
    }

    [Fact]
    public void Create_with_empty_message_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAiRun.Create(ValidConversationId, Guid.Empty, ValidAiModelId, ValidAiRunStatusId));

        Assert.Equal("chatMessageId", exception.ParamName);
    }

    [Fact]
    public void Create_with_empty_ai_model_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAiRun.Create(ValidConversationId, ValidMessageId, Guid.Empty, ValidAiRunStatusId));

        Assert.Equal("aiModelId", exception.ParamName);
    }

    [Fact]
    public void Create_with_empty_ai_run_status_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAiRun.Create(ValidConversationId, ValidMessageId, ValidAiModelId, Guid.Empty));

        Assert.Equal("aiRunStatusId", exception.ParamName);
    }

    [Fact]
    public void Create_sets_created_at_and_updated_at_equal()
    {
        var run = ChatAiRun.Create(
            ValidConversationId,
            ValidMessageId,
            ValidAiModelId,
            ValidAiRunStatusId);

        Assert.Equal(run.CreatedAt, run.UpdatedAt);
    }

    [Fact]
    public void UpdateStatus_changes_ai_run_status_id()
    {
        var run = ChatAiRun.Create(
            ValidConversationId,
            ValidMessageId,
            ValidAiModelId,
            ValidAiRunStatusId);

        run.UpdateStatus(ValidOtherStatusId);

        Assert.Equal(ValidOtherStatusId, run.AiRunStatusId);
    }

    [Fact]
    public void UpdateStatus_modifies_updated_at()
    {
        var run = ChatAiRun.Create(
            ValidConversationId,
            ValidMessageId,
            ValidAiModelId,
            ValidAiRunStatusId);
        var originalUpdatedAt = run.UpdatedAt;

        Thread.Sleep(2);
        run.UpdateStatus(ValidOtherStatusId);

        Assert.True(run.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void UpdateStatus_does_not_change_immutable_fields()
    {
        var run = ChatAiRun.Create(
            ValidConversationId,
            ValidMessageId,
            ValidAiModelId,
            ValidAiRunStatusId);
        var originalCreatedAt = run.CreatedAt;

        run.UpdateStatus(ValidOtherStatusId);

        Assert.Equal(ValidConversationId, run.ChatConversationId);
        Assert.Equal(ValidMessageId, run.ChatMessageId);
        Assert.Equal(ValidAiModelId, run.AiModelId);
        Assert.Equal(originalCreatedAt, run.CreatedAt);
    }

    [Fact]
    public async Task Create_with_valid_references_persists_run_without_side_effects()
    {
        var context = new ChatAiRunTestContext();
        var conversation = ChatConversation.Create(context.Status.Id);
        var message = ChatMessage.Create(
            conversation.Id,
            context.SenderTypeId,
            context.MessageTypeId,
            context.ParticipantId,
            "Mensaje");
        var aiModel = AiModel.Create(
            context.ProviderModelAiId,
            "Modelo",
            "model-key",
            0.001m,
            0.002m,
            1000,
            2000);
        var aiRunStatus = new AiRunStatusEntity("Pending");

        context.Conversations[conversation.Id] = conversation;
        context.Messages[message.Id] = message;
        context.AiModels[aiModel.Id] = aiModel;
        context.AiRunStatuses[aiRunStatus.Id] = aiRunStatus;

        var originalLastMessageAt = conversation.LastMessageAt;
        var handler = new CreateChatAiRunCommandHandler(context.UnitOfWork);
        var run = await handler.Handle(
            new CreateChatAiRunCommand(
                conversation.Id,
                message.Id,
                aiModel.Id,
                aiRunStatus.Id),
            CancellationToken.None);

        Assert.Contains(run.Id, context.Runs.Keys);
        Assert.Empty(context.Metrics);
        Assert.Empty(context.Errors);
        Assert.Equal(originalLastMessageAt, context.Conversations[conversation.Id].LastMessageAt);
        Assert.Equal("Mensaje", context.Messages[message.Id].Content);
    }

    [Fact]
    public async Task Create_with_missing_conversation_throws_not_found()
    {
        var context = new ChatAiRunTestContext();
        var handler = new CreateChatAiRunCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatAiRunCommand(
                    ValidConversationId,
                    ValidMessageId,
                    ValidAiModelId,
                    ValidAiRunStatusId),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_with_missing_message_throws_not_found()
    {
        var context = new ChatAiRunTestContext();
        var conversation = ChatConversation.Create(context.Status.Id);
        context.Conversations[conversation.Id] = conversation;
        var handler = new CreateChatAiRunCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatAiRunCommand(
                    conversation.Id,
                    ValidMessageId,
                    ValidAiModelId,
                    ValidAiRunStatusId),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_with_missing_ai_model_throws_not_found()
    {
        var context = new ChatAiRunTestContext();
        var conversation = ChatConversation.Create(context.Status.Id);
        var message = ChatMessage.Create(
            conversation.Id,
            context.SenderTypeId,
            context.MessageTypeId,
            context.ParticipantId,
            "Mensaje");
        context.Conversations[conversation.Id] = conversation;
        context.Messages[message.Id] = message;
        var handler = new CreateChatAiRunCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatAiRunCommand(
                    conversation.Id,
                    message.Id,
                    ValidAiModelId,
                    ValidAiRunStatusId),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_with_missing_ai_run_status_throws_not_found()
    {
        var context = new ChatAiRunTestContext();
        var conversation = ChatConversation.Create(context.Status.Id);
        var message = ChatMessage.Create(
            conversation.Id,
            context.SenderTypeId,
            context.MessageTypeId,
            context.ParticipantId,
            "Mensaje");
        var aiModel = AiModel.Create(
            context.ProviderModelAiId,
            "Modelo",
            "model-key",
            0.001m,
            0.002m,
            1000,
            2000);
        context.Conversations[conversation.Id] = conversation;
        context.Messages[message.Id] = message;
        context.AiModels[aiModel.Id] = aiModel;
        var handler = new CreateChatAiRunCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatAiRunCommand(
                    conversation.Id,
                    message.Id,
                    aiModel.Id,
                    ValidAiRunStatusId),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_with_message_from_other_conversation_throws_argument_exception()
    {
        var context = new ChatAiRunTestContext();
        var conversation = ChatConversation.Create(context.Status.Id);
        var otherConversation = ChatConversation.Create(context.Status.Id);
        var message = ChatMessage.Create(
            otherConversation.Id,
            context.SenderTypeId,
            context.MessageTypeId,
            context.ParticipantId,
            "Mensaje");
        var aiModel = AiModel.Create(
            context.ProviderModelAiId,
            "Modelo",
            "model-key",
            0.001m,
            0.002m,
            1000,
            2000);
        var aiRunStatus = new AiRunStatusEntity("Pending");

        context.Conversations[conversation.Id] = conversation;
        context.Messages[message.Id] = message;
        context.AiModels[aiModel.Id] = aiModel;
        context.AiRunStatuses[aiRunStatus.Id] = aiRunStatus;

        var handler = new CreateChatAiRunCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(
                new CreateChatAiRunCommand(
                    conversation.Id,
                    message.Id,
                    aiModel.Id,
                    aiRunStatus.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task Update_status_persists_change()
    {
        var context = new ChatAiRunTestContext();
        var run = ChatAiRun.Create(
            ValidConversationId,
            ValidMessageId,
            ValidAiModelId,
            ValidAiRunStatusId);
        var newStatus = new AiRunStatusEntity("Completed");
        context.Runs[run.Id] = run;
        context.AiRunStatuses[newStatus.Id] = newStatus;

        var handler = new UpdateChatAiRunStatusCommandHandler(context.UnitOfWork);
        var updated = await handler.Handle(
            new UpdateChatAiRunStatusCommand(run.Id, newStatus.Id),
            CancellationToken.None);

        Assert.Equal(newStatus.Id, updated.AiRunStatusId);
        Assert.Equal(newStatus.Id, context.Runs[run.Id].AiRunStatusId);
    }

    [Fact]
    public async Task Update_status_with_missing_run_throws_not_found()
    {
        var context = new ChatAiRunTestContext();
        var handler = new UpdateChatAiRunStatusCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new UpdateChatAiRunStatusCommand(ValidConversationId, ValidAiRunStatusId),
                CancellationToken.None));
    }

    [Fact]
    public async Task Update_status_with_missing_status_throws_not_found()
    {
        var context = new ChatAiRunTestContext();
        var run = ChatAiRun.Create(
            ValidConversationId,
            ValidMessageId,
            ValidAiModelId,
            ValidAiRunStatusId);
        context.Runs[run.Id] = run;
        var handler = new UpdateChatAiRunStatusCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new UpdateChatAiRunStatusCommand(run.Id, ValidOtherStatusId),
                CancellationToken.None));
    }

    [Fact]
    public async Task Get_by_id_missing_run_returns_null()
    {
        var context = new ChatAiRunTestContext();
        var handler = new GetChatAiRunByIdQueryHandler(context.UnitOfWork);

        var result = await handler.Handle(
            new GetChatAiRunByIdQuery(ValidConversationId),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Get_by_conversation_id_returns_repository_runs()
    {
        var context = new ChatAiRunTestContext();
        var first = ChatAiRun.Create(
            ValidConversationId,
            ValidMessageId,
            ValidAiModelId,
            ValidAiRunStatusId);
        var second = ChatAiRun.Create(
            ValidConversationId,
            ValidMessageId,
            ValidAiModelId,
            ValidAiRunStatusId);
        var other = ChatAiRun.Create(
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            ValidMessageId,
            ValidAiModelId,
            ValidAiRunStatusId);
        context.Runs[first.Id] = first;
        context.Runs[second.Id] = second;
        context.Runs[other.Id] = other;

        var handler = new GetChatAiRunsByConversationIdQueryHandler(context.UnitOfWork);
        var results = await handler.Handle(
            new GetChatAiRunsByConversationIdQuery(ValidConversationId),
            CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, run => run.Id == first.Id);
        Assert.Contains(results, run => run.Id == second.Id);
        Assert.DoesNotContain(results, run => run.Id == other.Id);
    }

    [Fact]
    public void Create_command_validator_rejects_empty_required_fields()
    {
        var validator = new CreateChatAiRunCommandValidator();
        var result = validator.Validate(
            new CreateChatAiRunCommand(Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 4);
    }

    private sealed class ChatAiRunTestContext
    {
        public Domain.ConversationStatuses.Entities.ConversationStatusEntity Status { get; } =
            new("Abierta");

        public Guid SenderTypeId { get; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public Guid MessageTypeId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public Guid ParticipantId { get; } = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public Guid ProviderModelAiId { get; } = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        public Dictionary<Guid, ChatConversation> Conversations { get; } = new();
        public Dictionary<Guid, ChatMessage> Messages { get; } = new();
        public Dictionary<Guid, AiModel> AiModels { get; } = new();
        public Dictionary<Guid, AiRunStatusEntity> AiRunStatuses { get; } = new();
        public Dictionary<Guid, ChatAiRun> Runs { get; } = new();
        public Dictionary<Guid, Domain.ChatAiRunMetrics.Entities.ChatAiRunMetrics> Metrics { get; } = new();
        public Dictionary<Guid, Domain.ChatAiRunErrors.Entities.ChatAiRunError> Errors { get; } = new();

        public IUnitOfWork UnitOfWork { get; }

        public ChatAiRunTestContext()
        {
            UnitOfWork = new FakeUnitOfWork(this);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly ChatAiRunTestContext _context;

        public FakeUnitOfWork(ChatAiRunTestContext context)
        {
            _context = context;
            ChatConversationsRepository = new FakeChatConversationRepository(context);
            ChatMessagesRepository = new FakeChatMessageRepository(context);
            AiModelsRepository = new FakeAiModelRepository(context);
            AiRunStatusesRepository = new FakeAiRunStatusRepository(context);
            ChatAiRunsRepository = new FakeChatAiRunRepository(context);
        }

        public IChatConversationRepository ChatConversationsRepository { get; }
        public IChatMessageRepository ChatMessagesRepository { get; }
        public IAiModelRepository AiModelsRepository { get; }
        public IAiRunStatusRepository AiRunStatusesRepository { get; }
        public IChatAiRunRepository ChatAiRunsRepository { get; }

        public IChatAiRunMetricsRepository ChatAiRunMetricsRepository => null!;
        public IChatAiRunErrorRepository ChatAiRunErrorsRepository => null!;
        public IChatAttachmentRepository ChatAttachmentsRepository => null!;
        public IChatParticipantRepository ChatParticipantsRepository => null!;

        public Application.Roles.Abstraction.IRolesRepository RolesRepository => null!;
        public Application.RolePermissions.Abstraction.IRolePermissionsRepository RolePermissionsRepository => null!;
        public Application.Modules.Abstraction.IModulesRepository ModulesRepository => null!;
        public Application.Species.Abstraction.ISpeciesRepository SpeciesRepository => null!;
        public Application.Races.Abstraction.IRaceRepository RacesRepository => null!;
        public Application.Pets.Abstraction.IPetRepository PetsRepository => null!;
        public Application.Users.Abstraction.IUsersRepository UsersRepository => null!;
        public Application.StatusAppointments.Abstraction.IStatusAppointmentRepository StatusAppointmentsRepository => null!;
        public Application.TypeServices.Abstraction.ITypeServiceRepository TypeServicesRepository => null!;
        public Application.Services.Abstraction.IServiceRepository ServicesRepository => null!;
        public Application.Specialties.Abstraction.ISpecialtyRepository SpecialtiesRepository => null!;
        public Application.ClientsPets.Abstraction.IClientPetRepository ClientPetsRepository => null!;
        public Application.Veterinarians.Abstraction.IVeterinarianRepository VeterinariansRepository => null!;
        public Application.Priorities.Abstraction.IPriorityRepository PrioritiesRepository => null!;
        public Application.SenderTypes.Abstraction.ISenderTypeRepository SenderTypesRepository => null!;
        public Application.ConversationStatuses.Abstraction.IConversationStatusRepository ConversationStatusesRepository => null!;
        public Application.MessageTypes.Abstraction.IMessageTypeRepository MessageTypesRepository => null!;
        public Application.EscalationStatuses.Abstraction.IEscalationStatusRepository EscalationStatusesRepository => null!;
        public Application.Appointments.Abstraction.IAppointmentRepository AppointmentsRepository => null!;
        public Application.AppointmentStatusHistories.Abstraction.IAppointmentStatusHistoryRepository AppointmentStatusHistoriesRepository => null!;
        public Application.MedicalRecords.Abstraction.IMedicalRecordRepository MedicalRecordsRepository => null!;
        public Application.Vaccinations.Abstraction.IVaccinationRepository VaccinationsRepository => null!;
        public Application.Notifications.Abstraction.INotificationRepository NotificationsRepository => null!;
        public Application.Diagnostics.Abstraction.IDiagnosticRepository DiagnosticsRepository => null!;
        public Application.AgentHumans.Abstraction.IAgentHumanRepository AgentHumansRepository => null!;
        public Application.ChatUserProfiles.Abstraction.IChatUserProfileRepository ChatUserProfilesRepository => null!;
        public Application.ChatConversationAssignments.Abstraction.IChatConversationAssignmentRepository ChatConversationAssignmentsRepository => null!;
        public Application.ChatConversationAiSettings.Abstraction.IChatConversationAiSettingRepository ChatConversationAiSettingsRepository => null!;
        public Application.ChatEscalations.Abstraction.IChatEscalationRepository ChatEscalationsRepository => null!;
        public Application.ChatEscalationStatusHistories.Abstraction.IChatEscalationStatusHistoryRepository ChatEscalationStatusHistoriesRepository => null!;
        public Application.ChatEscalationResolutions.Abstraction.IChatEscalationResolutionRepository ChatEscalationResolutionsRepository => null!;
        public Application.ChatEscalationAssignments.Abstraction.IChatEscalationAssignmentRepository ChatEscalationAssignmentsRepository => null!;
        public Application.ProviderModelsAi.Abstraction.IProviderModelAiRepository ProviderModelsAiRepository => null!;
        public Application.UserAccounts.Abstraction.IUserAccountsRepository UserAccountsRepository => null!;
        public Application.UserCredentials.Abstraction.IUserCredentialsRepository UserCredentialsRepository => null!;
        public Application.Clients.Abstraction.IClientRepository ClientsRepository => null!;
        public Application.UserTokens.Abstraction.IUserTokensRepository UserTokensRepository => null!;
        public Application.AccountStatements.Abstraction.IAccountStatementsRepository AccountStatementsRepository => null!;
        public Application.Availabilities.Abstraction.IAvailabilityRepository AvailabilitiesRepository => null!;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken = default)
            => action(cancellationToken);
    }

    private sealed class FakeChatConversationRepository : IChatConversationRepository
    {
        private readonly ChatAiRunTestContext _context;

        public FakeChatConversationRepository(ChatAiRunTestContext context) => _context = context;

        public Task<IReadOnlyCollection<ChatConversation>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ChatConversation>>(_context.Conversations.Values.ToArray());

        public Task<ChatConversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_context.Conversations.TryGetValue(id, out var conversation) ? conversation : null);

        public Task AddAsync(ChatConversation conversation, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(ChatConversation conversation, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeChatMessageRepository : IChatMessageRepository
    {
        private readonly ChatAiRunTestContext _context;

        public FakeChatMessageRepository(ChatAiRunTestContext context) => _context = context;

        public Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_context.Messages.TryGetValue(id, out var message) ? message : null);

        public Task<IReadOnlyCollection<ChatMessage>> GetAllByConversationIdAsync(
            Guid chatConversationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ChatMessage>>(Array.Empty<ChatMessage>());
    }

    private sealed class FakeAiModelRepository : IAiModelRepository
    {
        private readonly ChatAiRunTestContext _context;

        public FakeAiModelRepository(ChatAiRunTestContext context) => _context = context;

        public Task<AiModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_context.AiModels.TryGetValue(id, out var model) ? model : null);

        public Task<IReadOnlyCollection<AiModel>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<AiModel>>(_context.AiModels.Values.ToArray());

        public Task<IReadOnlyCollection<AiModel>> GetByProviderIdAsync(
            Guid providerModelAiId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<AiModel>>(Array.Empty<AiModel>());

        public Task AddAsync(AiModel aiModel, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(AiModel aiModel, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeAiRunStatusRepository : IAiRunStatusRepository
    {
        private readonly ChatAiRunTestContext _context;

        public FakeAiRunStatusRepository(ChatAiRunTestContext context) => _context = context;

        public Task<AiRunStatusEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_context.AiRunStatuses.TryGetValue(id, out var status) ? status : null);

        public Task<IReadOnlyCollection<AiRunStatusEntity>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<AiRunStatusEntity>>(_context.AiRunStatuses.Values.ToArray());

        public Task AddAsync(AiRunStatusEntity aiRunStatus, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(AiRunStatusEntity aiRunStatus, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(AiRunStatusEntity aiRunStatus, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeChatAiRunRepository : IChatAiRunRepository
    {
        private readonly ChatAiRunTestContext _context;

        public FakeChatAiRunRepository(ChatAiRunTestContext context) => _context = context;

        public Task AddAsync(ChatAiRun chatAiRun, CancellationToken cancellationToken = default)
        {
            _context.Runs[chatAiRun.Id] = chatAiRun;
            return Task.CompletedTask;
        }

        public Task<ChatAiRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_context.Runs.TryGetValue(id, out var run) ? run : null);

        public Task<IReadOnlyCollection<ChatAiRun>> GetAllByConversationIdAsync(
            Guid chatConversationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ChatAiRun>>(
                _context.Runs.Values
                    .Where(run => run.ChatConversationId == chatConversationId)
                    .OrderBy(run => run.CreatedAt)
                    .ToArray());

        public Task UpdateAsync(ChatAiRun chatAiRun, CancellationToken cancellationToken = default)
        {
            _context.Runs[chatAiRun.Id] = chatAiRun;
            return Task.CompletedTask;
        }
    }
}
