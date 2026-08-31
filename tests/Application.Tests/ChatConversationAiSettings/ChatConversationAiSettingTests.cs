using Application.AccountStatements.Abstraction;
using Application.AgentHumans.Abstraction;
using Application.AiModels.Abstraction;
using Application.AiRunStatuses.Abstraction;
using Application.Appointments.Abstraction;
using Application.AppointmentStatusHistories.Abstraction;
using Application.Availabilities.Abstraction;
using Application.ChatConversationAiSettings.Abstraction;
using Application.ChatConversationAssignments.Abstraction;
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
using Application.ChatConversationAiSettings.UseCase;
using Domain.AiModels.Entities;
using Domain.ChatConversationAiSettings.Entities;
using Domain.ChatConversations.Entities;
using Domain.ConversationStatuses.Entities;
using Xunit;

namespace Application.Tests.ChatConversationAiSettings;

public sealed class ChatConversationAiSettingTests
{
    private static readonly Guid ValidProviderId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Create_with_empty_conversation_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ChatConversationAiSetting.Create(Guid.Empty, true));

        Assert.Equal("conversationId", exception.ParamName);
    }

    [Fact]
    public void Create_sets_ai_enabled_and_default_model()
    {
        var conversationId = Guid.NewGuid();
        var modelId = Guid.NewGuid();

        var setting = ChatConversationAiSetting.Create(conversationId, true, modelId);

        Assert.Equal(conversationId, setting.ConversationId);
        Assert.True(setting.AiEnabled);
        Assert.Equal(modelId, setting.DefaultModelId);
        Assert.NotEqual(Guid.Empty, setting.Id);
    }

    [Fact]
    public void Update_changes_ai_enabled_and_default_model()
    {
        var conversationId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var setting = ChatConversationAiSetting.Create(conversationId, true, null);

        setting.Update(false, modelId);

        Assert.False(setting.AiEnabled);
        Assert.Equal(modelId, setting.DefaultModelId);
        Assert.NotNull(setting.UpdatedAt);
    }

    [Fact]
    public void Create_command_with_empty_conversation_id_fails_validation()
    {
        var validator = new CreateChatConversationAiSettingCommandValidator();

        var result = validator.Validate(
            new CreateChatConversationAiSettingCommand(Guid.Empty, true, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Create_missing_conversation_throws_not_found()
    {
        var context = new AiSettingTestContext();
        var handler = new CreateChatConversationAiSettingCommandHandler(context.UnitOfWork);
        var missingConversationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatConversationAiSettingCommand(missingConversationId, true, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_missing_model_throws_not_found()
    {
        var context = new AiSettingTestContext();
        var status = new ConversationStatusEntity("Abierta");
        var conversation = ChatConversation.Create(status.Id);
        context.Conversations[conversation.Id] = conversation;
        var missingModelId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var handler = new CreateChatConversationAiSettingCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatConversationAiSettingCommand(conversation.Id, true, missingModelId),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_with_valid_conversation_and_model_persists_setting()
    {
        var context = new AiSettingTestContext();
        var status = new ConversationStatusEntity("Abierta");
        var conversation = ChatConversation.Create(status.Id);
        var model = AiModel.Create(
            ValidProviderId,
            "GPT Test",
            "gpt-test",
            0.001m,
            0.002m,
            4096,
            8192);
        context.Conversations[conversation.Id] = conversation;
        context.Models[model.Id] = model;

        var handler = new CreateChatConversationAiSettingCommandHandler(context.UnitOfWork);
        var created = await handler.Handle(
            new CreateChatConversationAiSettingCommand(conversation.Id, true, model.Id),
            CancellationToken.None);

        Assert.Equal(conversation.Id, created.ConversationId);
        Assert.True(created.AiEnabled);
        Assert.Equal(model.Id, created.DefaultModelId);
        Assert.Contains(created.Id, context.Settings.Keys);
    }

    [Fact]
    public async Task Update_missing_setting_throws_not_found()
    {
        var context = new AiSettingTestContext();
        var handler = new UpdateChatConversationAiSettingCommandHandler(context.UnitOfWork);
        var missingSettingId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new UpdateChatConversationAiSettingCommand(missingSettingId, false, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Update_with_valid_setting_persists_changes()
    {
        var context = new AiSettingTestContext();
        var conversationId = Guid.NewGuid();
        var setting = ChatConversationAiSetting.Create(conversationId, true, null);
        context.Settings[setting.Id] = setting;

        var handler = new UpdateChatConversationAiSettingCommandHandler(context.UnitOfWork);
        var updated = await handler.Handle(
            new UpdateChatConversationAiSettingCommand(setting.Id, false, null),
            CancellationToken.None);

        Assert.False(updated.AiEnabled);
        Assert.False(context.Settings[setting.Id].AiEnabled);
    }

    private sealed class AiSettingTestContext
    {
        public Dictionary<Guid, ChatConversation> Conversations { get; } = new();
        public Dictionary<Guid, AiModel> Models { get; } = new();
        public Dictionary<Guid, ChatConversationAiSetting> Settings { get; } = new();

        public IUnitOfWork UnitOfWork { get; }

        public AiSettingTestContext()
        {
            UnitOfWork = new FakeUnitOfWork(this);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly AiSettingTestContext _context;

        public FakeUnitOfWork(AiSettingTestContext context)
        {
            _context = context;
            ChatConversationsRepository = new FakeChatConversationRepository(context);
            AiModelsRepository = new FakeAiModelRepository(context);
            ChatConversationAiSettingsRepository = new FakeAiSettingRepository(context);
        }

        public IChatConversationRepository ChatConversationsRepository { get; }
        public IAiModelRepository AiModelsRepository { get; }
        public IChatConversationAiSettingRepository ChatConversationAiSettingsRepository { get; }

        public IRolesRepository RolesRepository => null!;
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
        public ISenderTypeRepository SenderTypesRepository => null!;
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
        public IAgentHumanRepository AgentHumansRepository => null!;
        public IChatUserProfileRepository ChatUserProfilesRepository => null!;
        public IChatConversationAssignmentRepository ChatConversationAssignmentsRepository => null!;
        public IChatParticipantRepository ChatParticipantsRepository => null!;
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
        private readonly AiSettingTestContext _context;

        public FakeChatConversationRepository(AiSettingTestContext context)
        {
            _context = context;
        }

        public Task<IReadOnlyCollection<ChatConversation>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ChatConversation>>(_context.Conversations.Values.ToArray());

        public Task<ChatConversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(
                _context.Conversations.TryGetValue(id, out var conversation)
                    ? conversation
                    : null);

        public Task AddAsync(ChatConversation conversation, CancellationToken cancellationToken = default)
        {
            _context.Conversations[conversation.Id] = conversation;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ChatConversation conversation, CancellationToken cancellationToken = default)
        {
            _context.Conversations[conversation.Id] = conversation;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAiModelRepository : IAiModelRepository
    {
        private readonly AiSettingTestContext _context;

        public FakeAiModelRepository(AiSettingTestContext context)
        {
            _context = context;
        }

        public Task<IReadOnlyCollection<AiModel>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<AiModel>>(_context.Models.Values.ToArray());

        public Task<AiModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(
                _context.Models.TryGetValue(id, out var model)
                    ? model
                    : null);

        public Task<IReadOnlyCollection<AiModel>> GetByProviderIdAsync(
            Guid providerModelAiId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<AiModel>>(
                _context.Models.Values
                    .Where(model => model.ProviderModelAiId == providerModelAiId)
                    .ToArray());

        public Task AddAsync(AiModel model, CancellationToken cancellationToken = default)
        {
            _context.Models[model.Id] = model;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(AiModel model, CancellationToken cancellationToken = default)
        {
            _context.Models[model.Id] = model;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAiSettingRepository : IChatConversationAiSettingRepository
    {
        private readonly AiSettingTestContext _context;

        public FakeAiSettingRepository(AiSettingTestContext context)
        {
            _context = context;
        }

        public Task<IReadOnlyCollection<ChatConversationAiSetting>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ChatConversationAiSetting>>(_context.Settings.Values.ToArray());

        public Task<ChatConversationAiSetting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(
                _context.Settings.TryGetValue(id, out var setting)
                    ? setting
                    : null);

        public Task<ChatConversationAiSetting?> GetByConversationIdAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                _context.Settings.Values
                    .FirstOrDefault(setting => setting.ConversationId == conversationId));

        public Task AddAsync(ChatConversationAiSetting setting, CancellationToken cancellationToken = default)
        {
            _context.Settings[setting.Id] = setting;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ChatConversationAiSetting setting, CancellationToken cancellationToken = default)
        {
            _context.Settings[setting.Id] = setting;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ChatConversationAiSetting setting, CancellationToken cancellationToken = default)
        {
            _context.Settings.Remove(setting.Id);
            return Task.CompletedTask;
        }
    }
}
