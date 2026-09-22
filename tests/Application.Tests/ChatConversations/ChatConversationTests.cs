using Application.AgentHumans.Abstraction;
using Application.Appointments.Abstraction;
using Application.AppointmentStatusHistories.Abstraction;
using Application.Availabilities.Abstraction;
using Application.ChatConversations.Abstraction;
using Application.ChatConversations.UseCase;
using Application.ChatEscalations.Abstraction;
using Application.ChatMessages.Abstraction;
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
using NSubstitute;
using Xunit;

namespace Application.Tests.ChatConversations;

public sealed class ChatConversationTests
{
    private static readonly Guid ValidClosedById = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void Create_default_is_open_with_ai_enabled_and_web_channel()
    {
        var conversation = ChatConversation.Create();

        Assert.True(conversation.AiEnabled);
        Assert.False(conversation.Closed);
        Assert.Null(conversation.ClosedAt);
        Assert.Null(conversation.ClosedBy);
        Assert.NotEqual(Guid.Empty, conversation.Id);
        Assert.Equal("Web", conversation.Channel);
    }

    [Theory]
    [InlineData("Telegram")]
    [InlineData("Web")]
    public void Create_with_explicit_channel_persists_it(string channel)
    {
        var conversation = ChatConversation.Create(channel: channel);

        Assert.Equal(channel, conversation.Channel);
    }

    [Fact]
    public void Create_with_empty_channel_throws_argument_exception()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ChatConversation.Create(channel: "   "));

        Assert.Equal("channel", exception.ParamName);
    }

    [Fact]
    public void Close_sets_closed_state_and_closed_by_when_provided()
    {
        var conversation = ChatConversation.Create();

        conversation.Close(ValidClosedById);

        Assert.True(conversation.Closed);
        Assert.NotNull(conversation.ClosedAt);
        Assert.Equal(ValidClosedById, conversation.ClosedBy);
    }

    [Fact]
    public void Close_with_empty_closed_by_throws_argument_exception()
    {
        var conversation = ChatConversation.Create();

        var exception = Assert.Throws<ArgumentException>(
            () => conversation.Close(Guid.Empty));

        Assert.Equal("closedBy", exception.ParamName);
    }

    [Fact]
    public void Reopen_clears_closed_state()
    {
        var conversation = ChatConversation.Create();
        conversation.Close(ValidClosedById);

        conversation.Reopen();

        Assert.False(conversation.Closed);
        Assert.Null(conversation.ClosedAt);
        Assert.Null(conversation.ClosedBy);
    }

    [Fact]
    public void SetAiEnabled_and_UpdateLastMessageAt_update_state()
    {
        var conversation = ChatConversation.Create();

        conversation.SetAiEnabled(false);
        conversation.UpdateLastMessageAt(DateTime.UtcNow);

        Assert.False(conversation.AiEnabled);
        Assert.NotNull(conversation.LastMessageAt);
    }

    [Fact]
    public async Task Create_persists_conversation()
    {
        var context = new ChatConversationTestContext();

        var handler = new CreateChatConversationCommandHandler(context.UnitOfWork);
        var conversation = await handler.Handle(
            new CreateChatConversationCommand(Channel: "Web"),
            CancellationToken.None);

        Assert.Contains(conversation.Id, context.Conversations.Keys);
    }

    [Fact]
    public async Task Create_with_explicit_channel_persists_it_through_the_command()
    {
        var context = new ChatConversationTestContext();

        var handler = new CreateChatConversationCommandHandler(context.UnitOfWork);
        var conversation = await handler.Handle(
            new CreateChatConversationCommand(Channel: "Telegram"),
            CancellationToken.None);

        Assert.Equal("Telegram", conversation.Channel);
        Assert.Equal("Telegram", context.Conversations[conversation.Id].Channel);
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
        var conversation = ChatConversation.Create();
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
    public void Close_command_with_empty_closed_by_fails_validation()
    {
        var validator = new CloseChatConversationCommandValidator();

        var result = validator.Validate(new CloseChatConversationCommand(
            Guid.Parse("88888888-8888-8888-8888-888888888888"),
            Guid.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Close_when_already_closed_preserves_closed_at_and_closed_by()
    {
        var conversation = ChatConversation.Create();
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
        var conversation = ChatConversation.Create();
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
        var first = ChatConversation.Create();
        var second = ChatConversation.Create();
        context.Conversations[first.Id] = first;
        context.Conversations[second.Id] = second;

        var clientResolver = Substitute.For<IChatConversationClientResolver>();
        clientResolver.ResolveAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ChatConversationClientInfo.Empty);

        var handler = new GetAllChatConversationsQueryHandler(context.UnitOfWork, clientResolver);
        var results = await handler.Handle(new GetAllChatConversationsQuery(), CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, item => item.Conversation.Id == first.Id);
        Assert.Contains(results, item => item.Conversation.Id == second.Id);
    }

    [Fact]
    public async Task Get_all_includes_client_name_and_phone_when_a_client_participant_is_linked()
    {
        var context = new ChatConversationTestContext();
        var conversation = ChatConversation.Create();
        context.Conversations[conversation.Id] = conversation;

        var clientResolver = Substitute.For<IChatConversationClientResolver>();
        clientResolver.ResolveAsync(conversation.Id, Arg.Any<CancellationToken>())
            .Returns(new ChatConversationClientInfo(Guid.NewGuid(), "Ana Pérez", "3001234567"));

        var handler = new GetAllChatConversationsQueryHandler(context.UnitOfWork, clientResolver);
        var results = await handler.Handle(new GetAllChatConversationsQuery(), CancellationToken.None);

        var item = Assert.Single(results);
        Assert.Equal("Ana Pérez", item.ClientName);
        Assert.Equal("3001234567", item.ClientPhone);
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
            ChatConversationsRepository = new FakeChatConversationRepository(context);
        }

        public IChatConversationRepository ChatConversationsRepository { get; }
        public IChatParticipantRepository ChatParticipantsRepository => null!;
        public IChatMessageRepository ChatMessagesRepository => null!;

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
        public IEscalationStatusRepository EscalationStatusesRepository => null!;
        public IAppointmentRepository AppointmentsRepository => null!;
        public IAppointmentStatusHistoryRepository AppointmentStatusHistoriesRepository => null!;
        public IMedicalRecordRepository MedicalRecordsRepository => null!;
        public IVaccinationRepository VaccinationsRepository => null!;
        public INotificationRepository NotificationsRepository => null!;
        public IDiagnosticRepository DiagnosticsRepository => null!;
        public IAgentHumanRepository AgentHumansRepository => null!;
        public IChatEscalationRepository ChatEscalationsRepository => null!;
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
