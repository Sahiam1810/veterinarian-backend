using Application.AgentHumans.Abstraction;
using Application.Appointments.Abstraction;
using Application.AppointmentStatusHistories.Abstraction;
using Application.Availabilities.Abstraction;
using Application.ChatConversations.Abstraction;
using Application.ChatEscalations.Abstraction;
using Application.ChatMessages.Abstraction;
using Application.ChatMessages.UseCase;
using Application.ChatParticipants.Abstraction;
using Application.Clients.Abstraction;
using Application.ClientsPets.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Diagnostics.Abstraction;
using Application.EscalationStatuses.Abstraction;
using Application.MedicalRecords.Abstraction;
using Application.Notifications.Abstraction;
using Application.Pets.Abstraction;
using Application.Races.Abstraction;
using Application.Roles.Abstraction;
using Application.SenderTypes.Abstraction;
using Application.Services.Abstraction;
using Application.Specialties.Abstraction;
using Application.Species.Abstraction;
using Application.StatusAppointments.Abstraction;
using Application.TypeServices.Abstraction;
using Application.Users.Abstraction;
using Application.UserTokens.Abstraction;
using Application.Vaccinations.Abstraction;
using Application.Veterinarians.Abstraction;
using Domain.ChatConversations.Entities;
using Domain.ChatMessages.Entities;
using Domain.ChatParticipants.Entities;
using Domain.SenderTypes.Entities;
using MediatR;
using NSubstitute;
using Xunit;

namespace Application.Tests.ChatMessages;

public sealed class ChatMessageTests
{
    private static readonly Guid ValidConversationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ValidParticipantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ValidSenderTypeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ValidAgentHumanId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    [Fact]
    public void Create_with_valid_data_assigns_properties()
    {
        var message = ChatMessage.Create(
            ValidConversationId,
            ValidSenderTypeId,
            ValidParticipantId,
            "Hola",
            "{\"key\":\"value\"}");

        Assert.Equal(ValidConversationId, message.ChatConversationId);
        Assert.Equal(ValidSenderTypeId, message.SenderTypesId);
        Assert.Equal(ValidParticipantId, message.ChatParticipantId);
        Assert.Equal("Hola", message.Content);
        Assert.Equal("{\"key\":\"value\"}", message.Metadata);
        Assert.NotEqual(Guid.Empty, message.Id);
    }

    [Fact]
    public void Create_with_null_metadata_allows_optional_metadata()
    {
        var message = ChatMessage.Create(
            ValidConversationId,
            ValidSenderTypeId,
            ValidParticipantId,
            "Hola");

        Assert.Null(message.Metadata);
    }

    [Fact]
    public void Create_with_empty_conversation_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatMessage.Create(
                Guid.Empty,
                ValidSenderTypeId,
                ValidParticipantId,
                "Hola"));

        Assert.Equal("chatConversationId", exception.ParamName);
    }

    [Fact]
    public void Create_with_empty_sender_types_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatMessage.Create(
                ValidConversationId,
                Guid.Empty,
                ValidParticipantId,
                "Hola"));

        Assert.Equal("senderTypesId", exception.ParamName);
    }

    [Fact]
    public void Create_with_empty_chat_participant_id_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatMessage.Create(
                ValidConversationId,
                ValidSenderTypeId,
                Guid.Empty,
                "Hola"));

        Assert.Equal("chatParticipantId", exception.ParamName);
    }

    [Fact]
    public void Create_with_empty_content_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatMessage.Create(
                ValidConversationId,
                ValidSenderTypeId,
                ValidParticipantId,
                string.Empty));

        Assert.Equal("content", exception.ParamName);
    }

    [Fact]
    public void Create_with_whitespace_content_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ChatMessage.Create(
                ValidConversationId,
                ValidSenderTypeId,
                ValidParticipantId,
                "   "));

        Assert.Equal("content", exception.ParamName);
    }

    [Fact]
    public async Task Create_with_valid_references_persists_message_and_updates_conversation()
    {
        var context = new ChatMessageTestContext();
        var conversation = ChatConversation.Create();
        var senderType = new SenderTypeEntity("Usuario");
        var participant = ChatParticipant.Create(
            conversation.Id,
            senderType.Id,
            agentHumanId: ValidAgentHumanId);
        context.Conversations[conversation.Id] = conversation;
        context.SenderTypes[senderType.Id] = senderType;
        context.Participants[participant.Id] = participant;

        var publisher = Substitute.For<IPublisher>();
        var handler = new CreateChatMessageCommandHandler(context.UnitOfWork, publisher);
        var message = await handler.Handle(
            new CreateChatMessageCommand(
                conversation.Id,
                participant.Id,
                senderType.Id,
                "Mensaje de prueba",
                null),
            CancellationToken.None);

        Assert.Contains(message.Id, context.Messages.Keys);
        Assert.Equal(message.CreatedAt, context.Conversations[conversation.Id].LastMessageAt);
        await publisher.Received(1).Publish(
            Arg.Is<Application.ChatMessages.Events.ChatMessageCreatedNotification>(
                notification => notification.Message.Id == message.Id),
            CancellationToken.None);
    }

    [Fact]
    public async Task Create_with_missing_conversation_throws_not_found()
    {
        var context = new ChatMessageTestContext();
        var missingConversationId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var handler = new CreateChatMessageCommandHandler(context.UnitOfWork, Substitute.For<IPublisher>());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatMessageCommand(
                    missingConversationId,
                    ValidParticipantId,
                    ValidSenderTypeId,
                    "Hola",
                    null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_with_missing_participant_throws_not_found()
    {
        var context = new ChatMessageTestContext();
        var conversation = ChatConversation.Create();
        context.Conversations[conversation.Id] = conversation;
        var missingParticipantId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var handler = new CreateChatMessageCommandHandler(context.UnitOfWork, Substitute.For<IPublisher>());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatMessageCommand(
                    conversation.Id,
                    missingParticipantId,
                    ValidSenderTypeId,
                    "Hola",
                    null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_with_missing_sender_type_throws_not_found()
    {
        var context = new ChatMessageTestContext();
        var conversation = ChatConversation.Create();
        var senderType = new SenderTypeEntity("Usuario");
        var participant = ChatParticipant.Create(
            conversation.Id,
            senderType.Id,
            agentHumanId: ValidAgentHumanId);
        context.Conversations[conversation.Id] = conversation;
        context.Participants[participant.Id] = participant;
        var missingSenderTypeId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var handler = new CreateChatMessageCommandHandler(context.UnitOfWork, Substitute.For<IPublisher>());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new CreateChatMessageCommand(
                    conversation.Id,
                    participant.Id,
                    missingSenderTypeId,
                    "Hola",
                    null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Get_by_id_missing_message_returns_null()
    {
        var context = new ChatMessageTestContext();
        var handler = new GetChatMessageByIdQueryHandler(context.UnitOfWork);
        var missingMessageId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var result = await handler.Handle(
            new GetChatMessageByIdQuery(missingMessageId),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Get_by_id_existing_message_returns_message()
    {
        var context = new ChatMessageTestContext();
        var conversation = ChatConversation.Create();
        var senderType = new SenderTypeEntity("Usuario");
        var participant = ChatParticipant.Create(
            conversation.Id,
            senderType.Id,
            agentHumanId: ValidAgentHumanId);
        var message = ChatMessage.Create(
            conversation.Id,
            senderType.Id,
            participant.Id,
            "Contenido");
        context.Conversations[conversation.Id] = conversation;
        context.SenderTypes[senderType.Id] = senderType;
        context.Participants[participant.Id] = participant;
        context.Messages[message.Id] = message;

        var handler = new GetChatMessageByIdQueryHandler(context.UnitOfWork);
        var result = await handler.Handle(
            new GetChatMessageByIdQuery(message.Id),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(message.Id, result.Id);
    }

    [Fact]
    public async Task Get_all_by_conversation_id_missing_conversation_returns_empty()
    {
        var context = new ChatMessageTestContext();
        var handler = new GetChatMessagesByConversationIdQueryHandler(context.UnitOfWork);
        var missingConversationId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var results = await handler.Handle(
            new GetChatMessagesByConversationIdQuery(missingConversationId),
            CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task Get_all_by_conversation_id_existing_conversation_returns_messages()
    {
        var context = new ChatMessageTestContext();
        var conversation = ChatConversation.Create();
        var senderType = new SenderTypeEntity("Usuario");
        var participant = ChatParticipant.Create(
            conversation.Id,
            senderType.Id,
            agentHumanId: ValidAgentHumanId);
        var message1 = ChatMessage.Create(
            conversation.Id,
            senderType.Id,
            participant.Id,
            "Primer mensaje");
        var message2 = ChatMessage.Create(
            conversation.Id,
            senderType.Id,
            participant.Id,
            "Segundo mensaje");
        context.Conversations[conversation.Id] = conversation;
        context.SenderTypes[senderType.Id] = senderType;
        context.Participants[participant.Id] = participant;
        context.Messages[message1.Id] = message1;
        context.Messages[message2.Id] = message2;

        var handler = new GetChatMessagesByConversationIdQueryHandler(context.UnitOfWork);
        var results = await handler.Handle(
            new GetChatMessagesByConversationIdQuery(conversation.Id),
            CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, item => item.Id == message1.Id);
        Assert.Contains(results, item => item.Id == message2.Id);
    }

    [Fact]
    public void Create_command_validation_fails_for_invalid_guids()
    {
        var validator = new CreateChatMessageCommandValidator();

        var result = validator.Validate(new CreateChatMessageCommand(
            Guid.Empty,
            ValidParticipantId,
            ValidSenderTypeId,
            "Contenido",
            null));

        Assert.False(result.IsValid);
    }

    private sealed class ChatMessageTestContext
    {
        public Dictionary<Guid, ChatConversation> Conversations { get; } = new();
        public Dictionary<Guid, SenderTypeEntity> SenderTypes { get; } = new();
        public Dictionary<Guid, ChatParticipant> Participants { get; } = new();
        public Dictionary<Guid, ChatMessage> Messages { get; } = new();

        public IUnitOfWork UnitOfWork { get; }

        public ChatMessageTestContext()
        {
            UnitOfWork = new FakeUnitOfWork(this);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly ChatMessageTestContext _context;

        public FakeUnitOfWork(ChatMessageTestContext context)
        {
            _context = context;
            ChatConversationsRepository = new FakeChatConversationRepository(context);
            SenderTypesRepository = new FakeSenderTypeRepository(context);
            ChatParticipantsRepository = new FakeChatParticipantRepository(context);
            ChatMessagesRepository = new FakeChatMessageRepository(context);
        }

        public IChatConversationRepository ChatConversationsRepository { get; }
        public ISenderTypeRepository SenderTypesRepository { get; }
        public IChatParticipantRepository ChatParticipantsRepository { get; }
        public IChatMessageRepository ChatMessagesRepository { get; }
        public IChatEscalationRepository ChatEscalationsRepository => null!;

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
        public IEscalationStatusRepository EscalationStatusesRepository => null!;
        public IAppointmentRepository AppointmentsRepository => null!;
        public IAppointmentStatusHistoryRepository AppointmentStatusHistoriesRepository => null!;
        public IMedicalRecordRepository MedicalRecordsRepository => null!;
        public IVaccinationRepository VaccinationsRepository => null!;
        public INotificationRepository NotificationsRepository => null!;
        public IDiagnosticRepository DiagnosticsRepository => null!;
        public Application.Medications.Abstraction.IMedicationRepository MedicationsRepository => null!;
        public Application.Procedures.Abstraction.IProcedureRepository ProceduresRepository => null!;
        public Application.MedicationOrders.Abstraction.IMedicationOrderRepository MedicationOrdersRepository => null!;
        public Application.ProcedureOrders.Abstraction.IProcedureOrderRepository ProcedureOrdersRepository => null!;
        public IAgentHumanRepository AgentHumansRepository => null!;
        public IClientRepository ClientsRepository => null!;
        public IUserTokensRepository UserTokensRepository => null!;
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
        private readonly ChatMessageTestContext _context;

        public FakeChatConversationRepository(ChatMessageTestContext context)
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

    private sealed class FakeSenderTypeRepository : ISenderTypeRepository
    {
        private readonly ChatMessageTestContext _context;

        public FakeSenderTypeRepository(ChatMessageTestContext context)
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

    private sealed class FakeChatParticipantRepository : IChatParticipantRepository
    {
        private readonly ChatMessageTestContext _context;

        public FakeChatParticipantRepository(ChatMessageTestContext context)
        {
            _context = context;
        }

        public Task AddAsync(ChatParticipant participant, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

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
            => Task.CompletedTask;
    }

    private sealed class FakeChatMessageRepository : IChatMessageRepository
    {
        private readonly ChatMessageTestContext _context;

        public FakeChatMessageRepository(ChatMessageTestContext context)
        {
            _context = context;
        }

        public Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            _context.Messages[message.Id] = message;
            return Task.CompletedTask;
        }

        public Task<ChatMessage?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                _context.Messages.TryGetValue(id, out var message) ? message : null);

        public Task<IReadOnlyCollection<ChatMessage>> GetAllByConversationIdAsync(
            Guid chatConversationId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ChatMessage>>(
                _context.Messages.Values
                    .Where(message => message.ChatConversationId == chatConversationId)
                    .OrderBy(message => message.CreatedAt)
                    .ToArray());
    }
}
