using Application.ChatAiRunErrors.Abstraction;
using Application.ChatAiRunMetrics.Abstraction;
using Application.ChatAiRuns.Abstraction;
using Application.ChatAiRunErrors.UseCase;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.ChatAiRunErrors.Entities;
using Domain.ChatAiRuns.Entities;
using Xunit;

namespace Application.Tests.ChatAiRunErrors;

public sealed class ChatAiRunErrorTests
{
    private static readonly Guid ValidRunId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_with_valid_data_assigns_properties()
    {
        var error = ChatAiRunError.Create(
            ValidRunId,
            "Error de proveedor",
            "ERR_001",
            "prov-123");

        Assert.Equal(ValidRunId, error.ChatAiRunId);
        Assert.Equal("Error de proveedor", error.ErrorMessage);
        Assert.Equal("ERR_001", error.ErrorCode);
        Assert.Equal("prov-123", error.ProviderErrorId);
        Assert.NotEqual(Guid.Empty, error.Id);
    }

    [Fact]
    public void Create_with_empty_run_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAiRunError.Create(Guid.Empty, "Error"));

        Assert.Equal("chatAiRunId", exception.ParamName);
    }

    [Fact]
    public void Create_with_null_error_message_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAiRunError.Create(ValidRunId, null!));

        Assert.Equal("errorMessage", exception.ParamName);
    }

    [Fact]
    public void Create_with_empty_error_message_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAiRunError.Create(ValidRunId, string.Empty));

        Assert.Equal("errorMessage", exception.ParamName);
    }

    [Fact]
    public void Create_with_whitespace_error_message_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAiRunError.Create(ValidRunId, "   "));

        Assert.Equal("errorMessage", exception.ParamName);
    }

    [Fact]
    public void Create_with_null_error_code_is_valid()
    {
        var error = ChatAiRunError.Create(ValidRunId, "Error", null, null);

        Assert.Null(error.ErrorCode);
        Assert.Null(error.ProviderErrorId);
    }

    [Fact]
    public void Entity_has_no_update_method()
    {
        var updateMethod = typeof(ChatAiRunError).GetMethod("Update");

        Assert.Null(updateMethod);
    }

    [Fact]
    public async Task Create_for_existing_run_adds_and_saves()
    {
        var context = new ChatAiRunErrorTestContext();
        var run = ChatAiRun.Create(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Guid.Parse("55555555-5555-5555-5555-555555555555"));
        context.Runs[run.Id] = run;

        var handler = new CreateChatAiRunErrorCommandHandler(context.UnitOfWork);
        var error = await handler.Handle(
            new CreateChatAiRunErrorCommand(run.Id, "Fallo", "E001", null),
            CancellationToken.None);

        Assert.Contains(error.Id, context.Errors.Keys);
    }

    [Fact]
    public async Task Create_with_missing_run_throws_not_found()
    {
        var context = new ChatAiRunErrorTestContext();
        var handler = new CreateChatAiRunErrorCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatAiRunErrorCommand(ValidRunId, "Error", null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_allows_multiple_errors_for_same_run()
    {
        var context = new ChatAiRunErrorTestContext();
        var run = ChatAiRun.Create(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Guid.Parse("55555555-5555-5555-5555-555555555555"));
        context.Runs[run.Id] = run;
        var handler = new CreateChatAiRunErrorCommandHandler(context.UnitOfWork);

        var first = await handler.Handle(
            new CreateChatAiRunErrorCommand(run.Id, "Primer error", null, null),
            CancellationToken.None);
        var second = await handler.Handle(
            new CreateChatAiRunErrorCommand(run.Id, "Segundo error", null, null),
            CancellationToken.None);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, context.ErrorsByRunId[run.Id].Count);
    }

    [Fact]
    public async Task Get_by_id_missing_error_returns_null()
    {
        var context = new ChatAiRunErrorTestContext();
        var handler = new GetChatAiRunErrorByIdQueryHandler(context.UnitOfWork);

        var result = await handler.Handle(
            new GetChatAiRunErrorByIdQuery(ValidRunId),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Get_all_by_run_id_returns_ordered_errors()
    {
        var context = new ChatAiRunErrorTestContext();
        var runId = ValidRunId;
        var first = ChatAiRunError.Create(runId, "Primero");
        var second = ChatAiRunError.Create(runId, "Segundo");
        context.Errors[first.Id] = first;
        context.Errors[second.Id] = second;
        context.ErrorsByRunId[runId] = new List<ChatAiRunError> { first, second };

        var handler = new GetChatAiRunErrorsByChatAiRunIdQueryHandler(context.UnitOfWork);
        var results = await handler.Handle(
            new GetChatAiRunErrorsByChatAiRunIdQuery(runId),
            CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal(first.Id, results.First().Id);
        Assert.Equal(second.Id, results.Last().Id);
    }

    private sealed class ChatAiRunErrorTestContext
    {
        public Dictionary<Guid, ChatAiRun> Runs { get; } = new();
        public Dictionary<Guid, ChatAiRunError> Errors { get; } = new();
        public Dictionary<Guid, List<ChatAiRunError>> ErrorsByRunId { get; } = new();

        public IUnitOfWork UnitOfWork { get; }

        public ChatAiRunErrorTestContext()
        {
            UnitOfWork = new FakeUnitOfWork(this);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly ChatAiRunErrorTestContext _context;

        public FakeUnitOfWork(ChatAiRunErrorTestContext context)
        {
            _context = context;
            ChatAiRunsRepository = new FakeChatAiRunRepository(context);
            ChatAiRunErrorsRepository = new FakeChatAiRunErrorRepository(context);
        }

        public IChatAiRunRepository ChatAiRunsRepository { get; }
        public IChatAiRunErrorRepository ChatAiRunErrorsRepository { get; }
        public IChatAiRunMetricsRepository ChatAiRunMetricsRepository => null!;

        public Application.ChatConversations.Abstraction.IChatConversationRepository ChatConversationsRepository => null!;
        public Application.ChatMessages.Abstraction.IChatMessageRepository ChatMessagesRepository => null!;
        public Application.ChatAttachments.Abstraction.IChatAttachmentRepository ChatAttachmentsRepository => null!;
        public Application.ChatParticipants.Abstraction.IChatParticipantRepository ChatParticipantsRepository => null!;
        public Application.AiModels.Abstraction.IAiModelRepository AiModelsRepository => null!;
        public Application.AiRunStatuses.Abstraction.IAiRunStatusRepository AiRunStatusesRepository => null!;
        public Application.Roles.Abstraction.IRolesRepository RolesRepository => null!;
        public Application.RolePermissions.Abstraction.IRolePermissionsRepository RolePermissionsRepository => null!;
        public Application.UserPermissions.Abstraction.IUserPermissionsRepository UserPermissionsRepository => null!;
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
        private readonly ChatAiRunErrorTestContext _context;

        public FakeChatAiRunRepository(ChatAiRunErrorTestContext context) => _context = context;

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

    private sealed class FakeChatAiRunErrorRepository : IChatAiRunErrorRepository
    {
        private readonly ChatAiRunErrorTestContext _context;

        public FakeChatAiRunErrorRepository(ChatAiRunErrorTestContext context) => _context = context;

        public Task AddAsync(ChatAiRunError chatAiRunError, CancellationToken cancellationToken = default)
        {
            _context.Errors[chatAiRunError.Id] = chatAiRunError;
            if (!_context.ErrorsByRunId.TryGetValue(chatAiRunError.ChatAiRunId, out var list))
            {
                list = new List<ChatAiRunError>();
                _context.ErrorsByRunId[chatAiRunError.ChatAiRunId] = list;
            }

            list.Add(chatAiRunError);
            list.Sort((left, right) => left.CreatedAt.CompareTo(right.CreatedAt));
            return Task.CompletedTask;
        }

        public Task<ChatAiRunError?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_context.Errors.TryGetValue(id, out var error) ? error : null);

        public Task<IReadOnlyCollection<ChatAiRunError>> GetAllByChatAiRunIdAsync(
            Guid chatAiRunId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ChatAiRunError>>(
                _context.ErrorsByRunId.TryGetValue(chatAiRunId, out var errors)
                    ? errors.OrderBy(error => error.CreatedAt).ToArray()
                    : Array.Empty<ChatAiRunError>());
    }
}
