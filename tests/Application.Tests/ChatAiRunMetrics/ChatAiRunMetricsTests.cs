using Application.ChatAiRunErrors.Abstraction;
using Application.ChatAiRunMetrics.Abstraction;
using Application.ChatAiRuns.Abstraction;
using Application.ChatAiRunMetrics.UseCase;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.ChatAiRuns.Entities;
using ChatAiRunMetricsEntity = Domain.ChatAiRunMetrics.Entities.ChatAiRunMetrics;
using Xunit;

namespace Application.Tests.ChatAiRunMetrics;

public sealed class ChatAiRunMetricsTests
{
    private static readonly Guid ValidRunId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_with_valid_data_assigns_properties()
    {
        var metrics = ChatAiRunMetricsEntity.Create(ValidRunId, 10, 20, 30, 0.5m);

        Assert.Equal(ValidRunId, metrics.ChatAiRunId);
        Assert.Equal(10, metrics.PromptTokens);
        Assert.Equal(20, metrics.CompletionTokens);
        Assert.Equal(30, metrics.TotalTokens);
        Assert.Equal(0.5m, metrics.Cost);
        Assert.NotEqual(Guid.Empty, metrics.Id);
    }

    [Fact]
    public void Create_with_empty_run_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAiRunMetricsEntity.Create(Guid.Empty, 0, 0, 0, 0m));

        Assert.Equal("chatAiRunId", exception.ParamName);
    }

    [Fact]
    public void Create_with_negative_prompt_tokens_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAiRunMetricsEntity.Create(ValidRunId, -1, 0, 0, 0m));

        Assert.Equal("promptTokens", exception.ParamName);
    }

    [Fact]
    public void Create_with_negative_completion_tokens_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAiRunMetricsEntity.Create(ValidRunId, 0, -1, 0, 0m));

        Assert.Equal("completionTokens", exception.ParamName);
    }

    [Fact]
    public void Create_with_negative_total_tokens_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAiRunMetricsEntity.Create(ValidRunId, 0, 0, -1, 0m));

        Assert.Equal("totalTokens", exception.ParamName);
    }

    [Fact]
    public void Create_with_negative_cost_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAiRunMetricsEntity.Create(ValidRunId, 0, 0, 0, -0.1m));

        Assert.Equal("cost", exception.ParamName);
    }

    [Fact]
    public void Create_with_inconsistent_total_tokens_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAiRunMetricsEntity.Create(ValidRunId, 10, 20, 25, 0m));

        Assert.Equal("totalTokens", exception.ParamName);
    }

    [Fact]
    public void Create_with_zero_values_is_valid()
    {
        var metrics = ChatAiRunMetricsEntity.Create(ValidRunId, 0, 0, 0, 0m);

        Assert.Equal(0, metrics.PromptTokens);
        Assert.Equal(0, metrics.CompletionTokens);
        Assert.Equal(0, metrics.TotalTokens);
        Assert.Equal(0m, metrics.Cost);
    }

    [Fact]
    public void Entity_has_no_update_method()
    {
        var updateMethod = typeof(ChatAiRunMetricsEntity).GetMethod("Update");

        Assert.Null(updateMethod);
    }

    [Fact]
    public async Task Create_for_existing_run_adds_and_saves()
    {
        var context = new ChatAiRunMetricsTestContext();
        var run = ChatAiRun.Create(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Guid.Parse("55555555-5555-5555-5555-555555555555"));
        context.Runs[run.Id] = run;

        var handler = new CreateChatAiRunMetricsCommandHandler(context.UnitOfWork);
        var metrics = await handler.Handle(
            new CreateChatAiRunMetricsCommand(run.Id, 5, 15, 20, 1.25m),
            CancellationToken.None);

        Assert.Contains(metrics.Id, context.Metrics.Keys);
    }

    [Fact]
    public async Task Create_with_missing_run_throws_not_found()
    {
        var context = new ChatAiRunMetricsTestContext();
        var handler = new CreateChatAiRunMetricsCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatAiRunMetricsCommand(ValidRunId, 1, 1, 2, 0m),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_with_existing_metrics_throws_conflict()
    {
        var context = new ChatAiRunMetricsTestContext();
        var run = ChatAiRun.Create(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Guid.Parse("55555555-5555-5555-5555-555555555555"));
        context.Runs[run.Id] = run;
        var existing = ChatAiRunMetricsEntity.Create(run.Id, 1, 1, 2, 0m);
        context.Metrics[existing.Id] = existing;
        context.MetricsByRunId[run.Id] = existing;

        var handler = new CreateChatAiRunMetricsCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new CreateChatAiRunMetricsCommand(run.Id, 2, 2, 4, 0m),
                CancellationToken.None));
    }

    [Fact]
    public async Task Get_by_id_missing_metrics_returns_null()
    {
        var context = new ChatAiRunMetricsTestContext();
        var handler = new GetChatAiRunMetricsByIdQueryHandler(context.UnitOfWork);

        var result = await handler.Handle(
            new GetChatAiRunMetricsByIdQuery(ValidRunId),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Get_by_run_id_missing_metrics_returns_null()
    {
        var context = new ChatAiRunMetricsTestContext();
        var handler = new GetChatAiRunMetricsByChatAiRunIdQueryHandler(context.UnitOfWork);

        var result = await handler.Handle(
            new GetChatAiRunMetricsByChatAiRunIdQuery(ValidRunId),
            CancellationToken.None);

        Assert.Null(result);
    }

    private sealed class ChatAiRunMetricsTestContext
    {
        public Dictionary<Guid, ChatAiRun> Runs { get; } = new();
        public Dictionary<Guid, ChatAiRunMetricsEntity> Metrics { get; } = new();
        public Dictionary<Guid, ChatAiRunMetricsEntity> MetricsByRunId { get; } = new();

        public IUnitOfWork UnitOfWork { get; }

        public ChatAiRunMetricsTestContext()
        {
            UnitOfWork = new FakeUnitOfWork(this);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly ChatAiRunMetricsTestContext _context;

        public FakeUnitOfWork(ChatAiRunMetricsTestContext context)
        {
            _context = context;
            ChatAiRunsRepository = new FakeChatAiRunRepository(context);
            ChatAiRunMetricsRepository = new FakeChatAiRunMetricsRepository(context);
        }

        public IChatAiRunRepository ChatAiRunsRepository { get; }
        public IChatAiRunMetricsRepository ChatAiRunMetricsRepository { get; }
        public IChatAiRunErrorRepository ChatAiRunErrorsRepository => null!;

        public Application.ChatConversations.Abstraction.IChatConversationRepository ChatConversationsRepository => null!;
        public Application.ChatMessages.Abstraction.IChatMessageRepository ChatMessagesRepository => null!;
        public Application.ChatAttachments.Abstraction.IChatAttachmentRepository ChatAttachmentsRepository => null!;
        public Application.ChatParticipants.Abstraction.IChatParticipantRepository ChatParticipantsRepository => null!;
        public Application.AiModels.Abstraction.IAiModelRepository AiModelsRepository => null!;
        public Application.AiRunStatuses.Abstraction.IAiRunStatusRepository AiRunStatusesRepository => null!;
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

    private sealed class FakeChatAiRunRepository : IChatAiRunRepository
    {
        private readonly ChatAiRunMetricsTestContext _context;

        public FakeChatAiRunRepository(ChatAiRunMetricsTestContext context) => _context = context;

        public Task AddAsync(ChatAiRun chatAiRun, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<ChatAiRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_context.Runs.TryGetValue(id, out var run) ? run : null);

        public Task<IReadOnlyCollection<ChatAiRun>> GetAllByConversationIdAsync(
            Guid chatConversationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ChatAiRun>>(Array.Empty<ChatAiRun>());

        public Task UpdateAsync(ChatAiRun chatAiRun, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeChatAiRunMetricsRepository : IChatAiRunMetricsRepository
    {
        private readonly ChatAiRunMetricsTestContext _context;

        public FakeChatAiRunMetricsRepository(ChatAiRunMetricsTestContext context) => _context = context;

        public Task AddAsync(ChatAiRunMetricsEntity chatAiRunMetrics, CancellationToken cancellationToken = default)
        {
            _context.Metrics[chatAiRunMetrics.Id] = chatAiRunMetrics;
            _context.MetricsByRunId[chatAiRunMetrics.ChatAiRunId] = chatAiRunMetrics;
            return Task.CompletedTask;
        }

        public Task<ChatAiRunMetricsEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_context.Metrics.TryGetValue(id, out var metrics) ? metrics : null);

        public Task<ChatAiRunMetricsEntity?> GetByChatAiRunIdAsync(
            Guid chatAiRunId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                _context.MetricsByRunId.TryGetValue(chatAiRunId, out var metrics) ? metrics : null);

        public Task<bool> ExistsByChatAiRunIdAsync(
            Guid chatAiRunId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_context.MetricsByRunId.ContainsKey(chatAiRunId));
    }
}
