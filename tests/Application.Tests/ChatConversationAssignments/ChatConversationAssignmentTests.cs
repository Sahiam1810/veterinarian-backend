using Application.AccountStatements.Abstraction;
using Application.AgentHumans.Abstraction;
using Application.AiModels.Abstraction;
using Application.AiRunStatuses.Abstraction;
using Application.Appointments.Abstraction;
using Application.AppointmentStatusHistories.Abstraction;
using Application.Availabilities.Abstraction;
using Application.ChatConversationAssignments.Abstraction;
using Application.ChatConversationAiSettings.Abstraction;
using Application.ChatAttachments.Abstraction;
using Application.ChatMessages.Abstraction;
using Application.ChatConversations.Abstraction;
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
using Application.ChatConversationAssignments.UseCase;
using Domain.AgentHumans.Entities;
using Domain.ChatConversationAssignments.Entities;
using Domain.ChatConversations.Entities;
using Domain.ConversationStatuses.Entities;
using Xunit;

namespace Application.Tests.ChatConversationAssignments;

public sealed class ChatConversationAssignmentTests
{
    private static readonly Guid ValidUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_with_empty_conversation_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ChatConversationAssignment.Create(Guid.Empty));

        Assert.Equal("chatConversationId", exception.ParamName);
    }

    [Fact]
    public void Assign_sets_agent_and_assigned_at()
    {
        var conversationId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var assignment = ChatConversationAssignment.Create(conversationId);

        assignment.Assign(agentId);

        Assert.Equal(agentId, assignment.AgentHumanId);
        Assert.NotNull(assignment.AssignedAt);
        Assert.Null(assignment.UnassignedAt);
    }

    [Fact]
    public void Unassign_clears_agent_and_sets_unassigned_at()
    {
        var conversationId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var assignment = ChatConversationAssignment.Create(conversationId, agentId);

        assignment.Unassign();

        Assert.Null(assignment.AgentHumanId);
        Assert.NotNull(assignment.UnassignedAt);
    }

    [Fact]
    public void Create_command_with_empty_conversation_id_fails_validation()
    {
        var validator = new CreateChatConversationAssignmentCommandValidator();

        var result = validator.Validate(
            new CreateChatConversationAssignmentCommand(Guid.Empty, null, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Create_missing_conversation_throws_not_found()
    {
        var context = new AssignmentTestContext();
        var handler = new CreateChatConversationAssignmentCommandHandler(context.UnitOfWork);
        var missingConversationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatConversationAssignmentCommand(missingConversationId, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_duplicate_assignment_throws_argument_exception()
    {
        var context = new AssignmentTestContext();
        var status = new ConversationStatusEntity("Abierta");
        var conversation = ChatConversation.Create(status.Id);
        context.Conversations[conversation.Id] = conversation;
        context.Assignments[conversation.Id] = ChatConversationAssignment.Create(conversation.Id);

        var handler = new CreateChatConversationAssignmentCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(
                new CreateChatConversationAssignmentCommand(conversation.Id, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_with_valid_conversation_persists_assignment()
    {
        var context = new AssignmentTestContext();
        var status = new ConversationStatusEntity("Abierta");
        var conversation = ChatConversation.Create(status.Id);
        context.Conversations[conversation.Id] = conversation;

        var handler = new CreateChatConversationAssignmentCommandHandler(context.UnitOfWork);
        var created = await handler.Handle(
            new CreateChatConversationAssignmentCommand(conversation.Id, null, null),
            CancellationToken.None);

        Assert.Equal(conversation.Id, created.ChatConversationId);
        Assert.True(context.Assignments.ContainsKey(conversation.Id));
    }

    [Fact]
    public async Task Create_with_missing_agent_throws_not_found()
    {
        var context = new AssignmentTestContext();
        var status = new ConversationStatusEntity("Abierta");
        var conversation = ChatConversation.Create(status.Id);
        context.Conversations[conversation.Id] = conversation;
        var missingAgentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var handler = new CreateChatConversationAssignmentCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatConversationAssignmentCommand(conversation.Id, missingAgentId, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_with_valid_agent_persists_assignment()
    {
        var context = new AssignmentTestContext();
        var status = new ConversationStatusEntity("Abierta");
        var conversation = ChatConversation.Create(status.Id);
        var agent = AgentHuman.Create(ValidUserId);
        context.Conversations[conversation.Id] = conversation;
        context.Agents[agent.Id] = agent;

        var handler = new CreateChatConversationAssignmentCommandHandler(context.UnitOfWork);
        var created = await handler.Handle(
            new CreateChatConversationAssignmentCommand(conversation.Id, agent.Id, null),
            CancellationToken.None);

        Assert.Equal(agent.Id, created.AgentHumanId);
        Assert.NotNull(created.AssignedAt);
    }

    [Fact]
    public async Task Update_missing_assignment_throws_not_found()
    {
        var context = new AssignmentTestContext();
        var handler = new UpdateChatConversationAssignmentCommandHandler(context.UnitOfWork);
        var missingConversationId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new UpdateChatConversationAssignmentCommand(missingConversationId, null, null, null),
                CancellationToken.None));
    }

    private sealed class AssignmentTestContext
    {
        public Dictionary<Guid, ChatConversation> Conversations { get; } = new();
        public Dictionary<Guid, AgentHuman> Agents { get; } = new();
        public Dictionary<Guid, ChatConversationAssignment> Assignments { get; } = new();

        public IUnitOfWork UnitOfWork { get; }

        public AssignmentTestContext()
        {
            UnitOfWork = new FakeUnitOfWork(this);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly AssignmentTestContext _context;

        public FakeUnitOfWork(AssignmentTestContext context)
        {
            _context = context;
            ChatConversationsRepository = new FakeChatConversationRepository(context);
            AgentHumansRepository = new FakeAgentHumanRepository(context);
            ChatConversationAssignmentsRepository = new FakeAssignmentRepository(context);
        }

        public IChatConversationRepository ChatConversationsRepository { get; }
        public IAgentHumanRepository AgentHumansRepository { get; }
        public IChatConversationAssignmentRepository ChatConversationAssignmentsRepository { get; }

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
        public IAiModelRepository AiModelsRepository => null!;
        public IChatUserProfileRepository ChatUserProfilesRepository => null!;
        public IChatConversationAiSettingRepository ChatConversationAiSettingsRepository => null!;
        public IChatParticipantRepository ChatParticipantsRepository => null!;
        public IChatMessageRepository ChatMessagesRepository => null!;
        public IChatAttachmentRepository ChatAttachmentsRepository => null!;
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
        private readonly AssignmentTestContext _context;

        public FakeChatConversationRepository(AssignmentTestContext context)
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

    private sealed class FakeAgentHumanRepository : IAgentHumanRepository
    {
        private readonly AssignmentTestContext _context;

        public FakeAgentHumanRepository(AssignmentTestContext context)
        {
            _context = context;
        }

        public Task<IReadOnlyCollection<AgentHuman>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<AgentHuman>>(_context.Agents.Values.ToArray());

        public Task<AgentHuman?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(
                _context.Agents.TryGetValue(id, out var agent)
                    ? agent
                    : null);

        public Task<IReadOnlyCollection<AgentHuman>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<AgentHuman>>(
                _context.Agents.Values.Where(agent => agent.UserId == userId).ToArray());

        public Task AddAsync(AgentHuman agent, CancellationToken cancellationToken = default)
        {
            _context.Agents[agent.Id] = agent;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(AgentHuman agent, CancellationToken cancellationToken = default)
        {
            _context.Agents[agent.Id] = agent;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAssignmentRepository : IChatConversationAssignmentRepository
    {
        private readonly AssignmentTestContext _context;

        public FakeAssignmentRepository(AssignmentTestContext context)
        {
            _context = context;
        }

        public Task<IReadOnlyCollection<ChatConversationAssignment>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ChatConversationAssignment>>(_context.Assignments.Values.ToArray());

        public Task<ChatConversationAssignment?> GetByConversationIdAsync(
            Guid chatConversationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                _context.Assignments.TryGetValue(chatConversationId, out var assignment)
                    ? assignment
                    : null);

        public Task<IReadOnlyCollection<ChatConversationAssignment>> GetByAgentHumanIdAsync(
            Guid agentHumanId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ChatConversationAssignment>>(
                _context.Assignments.Values
                    .Where(assignment => assignment.AgentHumanId == agentHumanId)
                    .ToArray());

        public Task<bool> ExistsByConversationIdAsync(
            Guid chatConversationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_context.Assignments.ContainsKey(chatConversationId));

        public Task AddAsync(ChatConversationAssignment assignment, CancellationToken cancellationToken = default)
        {
            _context.Assignments[assignment.ChatConversationId] = assignment;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ChatConversationAssignment assignment, CancellationToken cancellationToken = default)
        {
            _context.Assignments[assignment.ChatConversationId] = assignment;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ChatConversationAssignment assignment, CancellationToken cancellationToken = default)
        {
            _context.Assignments.Remove(assignment.ChatConversationId);
            return Task.CompletedTask;
        }
    }
}
