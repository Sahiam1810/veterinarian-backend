using Application.AccountStatements.Abstraction;
using Application.AgentHumans.Abstraction;
using Application.AiModels.Abstraction;
using Application.AiRunStatuses.Abstraction;
using Application.Appointments.Abstraction;
using Application.AppointmentStatusHistories.Abstraction;
using Application.Availabilities.Abstraction;
using Application.ChatAiRunErrors.Abstraction;
using Application.ChatAiRunMetrics.Abstraction;
using Application.ChatAiRuns.Abstraction;
using Application.ChatAttachments.Abstraction;
using Application.ChatAttachments.UseCase;
using Application.ChatConversationAssignments.Abstraction;
using Application.ChatConversationAiSettings.Abstraction;
using Application.ChatConversations.Abstraction;
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
using Domain.ChatAttachments.Entities;
using Domain.ChatMessages.Entities;
using Xunit;

namespace Application.Tests.ChatAttachments;

public sealed class ChatAttachmentTests
{
    private static readonly Guid ValidMessageId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ValidConversationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ValidSenderTypeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ValidMessageTypeId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ValidParticipantId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    [Fact]
    public void Create_with_valid_data_assigns_properties()
    {
        var attachment = ChatAttachment.Create(
            ValidMessageId,
            "https://example.com/file.pdf",
            "application/pdf",
            "file.pdf");

        Assert.Equal(ValidMessageId, attachment.ChatMessageId);
        Assert.Equal("https://example.com/file.pdf", attachment.FileUrl);
        Assert.Equal("application/pdf", attachment.FileType);
        Assert.Equal("file.pdf", attachment.FileName);
        Assert.NotEqual(Guid.Empty, attachment.Id);
    }

    [Fact]
    public void Create_with_empty_message_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAttachment.Create(
                Guid.Empty,
                "https://example.com/file.pdf",
                "application/pdf",
                "file.pdf"));

        Assert.Equal("chatMessageId", exception.ParamName);
    }

    [Fact]
    public void Create_with_empty_file_url_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAttachment.Create(
                ValidMessageId,
                string.Empty,
                "application/pdf",
                "file.pdf"));

        Assert.Equal("fileUrl", exception.ParamName);
    }

    [Fact]
    public void Create_with_whitespace_file_url_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAttachment.Create(
                ValidMessageId,
                "   ",
                "application/pdf",
                "file.pdf"));

        Assert.Equal("fileUrl", exception.ParamName);
    }

    [Fact]
    public void Create_with_empty_file_type_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAttachment.Create(
                ValidMessageId,
                "https://example.com/file.pdf",
                string.Empty,
                "file.pdf"));

        Assert.Equal("fileType", exception.ParamName);
    }

    [Fact]
    public void Create_with_empty_file_name_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatAttachment.Create(
                ValidMessageId,
                "https://example.com/file.pdf",
                "application/pdf",
                string.Empty));

        Assert.Equal("fileName", exception.ParamName);
    }

    [Fact]
    public async Task Create_with_existing_message_persists_attachment()
    {
        var context = new ChatAttachmentTestContext();
        var message = ChatMessage.Create(
            ValidConversationId,
            ValidSenderTypeId,
            ValidMessageTypeId,
            ValidParticipantId,
            "Mensaje");
        context.Messages[message.Id] = message;

        var handler = new CreateChatAttachmentCommandHandler(context.UnitOfWork);
        var attachment = await handler.Handle(
            new CreateChatAttachmentCommand(
                message.Id,
                "https://example.com/file.pdf",
                "application/pdf",
                "file.pdf"),
            CancellationToken.None);

        Assert.Contains(attachment.Id, context.Attachments.Keys);
        Assert.Equal(message.Id, attachment.ChatMessageId);
    }

    [Fact]
    public async Task Create_with_missing_message_throws_not_found()
    {
        var context = new ChatAttachmentTestContext();
        var missingMessageId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var handler = new CreateChatAttachmentCommandHandler(context.UnitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatAttachmentCommand(
                    missingMessageId,
                    "https://example.com/file.pdf",
                    "application/pdf",
                    "file.pdf"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Get_by_id_missing_attachment_returns_null()
    {
        var context = new ChatAttachmentTestContext();
        var handler = new GetChatAttachmentByIdQueryHandler(context.UnitOfWork);
        var missingAttachmentId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        var result = await handler.Handle(
            new GetChatAttachmentByIdQuery(missingAttachmentId),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Get_by_message_id_returns_repository_attachments()
    {
        var context = new ChatAttachmentTestContext();
        var message = ChatMessage.Create(
            ValidConversationId,
            ValidSenderTypeId,
            ValidMessageTypeId,
            ValidParticipantId,
            "Mensaje");
        var otherMessage = ChatMessage.Create(
            ValidConversationId,
            ValidSenderTypeId,
            ValidMessageTypeId,
            ValidParticipantId,
            "Otro");
        var first = ChatAttachment.Create(
            message.Id,
            "https://example.com/a.pdf",
            "application/pdf",
            "a.pdf");
        var second = ChatAttachment.Create(
            message.Id,
            "https://example.com/b.pdf",
            "application/pdf",
            "b.pdf");
        var other = ChatAttachment.Create(
            otherMessage.Id,
            "https://example.com/c.pdf",
            "application/pdf",
            "c.pdf");
        context.Attachments[first.Id] = first;
        context.Attachments[second.Id] = second;
        context.Attachments[other.Id] = other;

        var handler = new GetChatAttachmentsByMessageIdQueryHandler(context.UnitOfWork);
        var results = await handler.Handle(
            new GetChatAttachmentsByMessageIdQuery(message.Id),
            CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, attachment => attachment.Id == first.Id);
        Assert.Contains(results, attachment => attachment.Id == second.Id);
        Assert.DoesNotContain(results, attachment => attachment.Id == other.Id);
    }

    [Fact]
    public void Create_command_validator_rejects_empty_required_fields()
    {
        var validator = new CreateChatAttachmentCommandValidator();
        var result = validator.Validate(
            new CreateChatAttachmentCommand(
                Guid.Empty,
                string.Empty,
                string.Empty,
                string.Empty));

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 4);
    }

    [Fact]
    public void Get_by_id_query_validator_rejects_empty_id()
    {
        var validator = new GetChatAttachmentByIdQueryValidator();
        var result = validator.Validate(new GetChatAttachmentByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Get_by_message_id_query_validator_rejects_empty_message_id()
    {
        var validator = new GetChatAttachmentsByMessageIdQueryValidator();
        var result = validator.Validate(new GetChatAttachmentsByMessageIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
    }

    private sealed class ChatAttachmentTestContext
    {
        public Dictionary<Guid, ChatMessage> Messages { get; } = new();
        public Dictionary<Guid, ChatAttachment> Attachments { get; } = new();

        public IUnitOfWork UnitOfWork { get; }

        public ChatAttachmentTestContext()
        {
            UnitOfWork = new FakeUnitOfWork(this);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly ChatAttachmentTestContext _context;

        public FakeUnitOfWork(ChatAttachmentTestContext context)
        {
            _context = context;
            ChatMessagesRepository = new FakeChatMessageRepository(context);
            ChatAttachmentsRepository = new FakeChatAttachmentRepository(context);
        }

        public IChatMessageRepository ChatMessagesRepository { get; }
        public IChatAttachmentRepository ChatAttachmentsRepository { get; }
        public IChatEscalationRepository ChatEscalationsRepository => null!;
        public IChatEscalationStatusHistoryRepository ChatEscalationStatusHistoriesRepository => null!;
        public IChatEscalationResolutionRepository ChatEscalationResolutionsRepository => null!;
        public IChatEscalationAssignmentRepository ChatEscalationAssignmentsRepository => null!;
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
        public IAiModelRepository AiModelsRepository => null!;
        public IChatUserProfileRepository ChatUserProfilesRepository => null!;
        public IChatConversationRepository ChatConversationsRepository => null!;
        public IChatConversationAssignmentRepository ChatConversationAssignmentsRepository => null!;
        public IChatConversationAiSettingRepository ChatConversationAiSettingsRepository => null!;
        public IChatParticipantRepository ChatParticipantsRepository => null!;
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

    private sealed class FakeChatMessageRepository : IChatMessageRepository
    {
        private readonly ChatAttachmentTestContext _context;

        public FakeChatMessageRepository(ChatAttachmentTestContext context)
        {
            _context = context;
        }

        public Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<ChatMessage?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                _context.Messages.TryGetValue(id, out var message) ? message : null);

        public Task<IReadOnlyCollection<ChatMessage>> GetAllByConversationIdAsync(
            Guid chatConversationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ChatMessage>>(Array.Empty<ChatMessage>());
    }

    private sealed class FakeChatAttachmentRepository : IChatAttachmentRepository
    {
        private readonly ChatAttachmentTestContext _context;

        public FakeChatAttachmentRepository(ChatAttachmentTestContext context)
        {
            _context = context;
        }

        public Task AddAsync(ChatAttachment attachment, CancellationToken cancellationToken = default)
        {
            _context.Attachments[attachment.Id] = attachment;
            return Task.CompletedTask;
        }

        public Task<ChatAttachment?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                _context.Attachments.TryGetValue(id, out var attachment) ? attachment : null);

        public Task<IReadOnlyCollection<ChatAttachment>> GetAllByMessageIdAsync(
            Guid chatMessageId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ChatAttachment>>(
                _context.Attachments.Values
                    .Where(attachment => attachment.ChatMessageId == chatMessageId)
                    .OrderBy(attachment => attachment.CreatedAt)
                    .ToArray());
    }
}
