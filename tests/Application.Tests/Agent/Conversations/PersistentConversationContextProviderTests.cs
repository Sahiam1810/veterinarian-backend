using Application.Agent.Abstractions;
using Application.Agent.Conversations;
using Application.Agent.Errors;
using Application.ChatConversations.Abstraction;
using Application.ChatParticipants.Abstraction;
using Application.ChatUserProfiles.Abstraction;
using Application.Common.Abstractions;
using Application.ConversationStatuses.Abstraction;
using Application.SenderTypes.Abstraction;
using Application.Users.Abstraction;
using Domain.ChatConversations.Entities;
using Domain.ChatParticipants.Entities;
using Domain.ChatUserProfiles.Entities;
using Domain.ConversationStatuses.Entities;
using Domain.SenderTypes.Entities;
using NSubstitute;
using Xunit;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Tests.Agent.Conversations;

public sealed class PersistentConversationContextProviderTests
{
    private static readonly Guid PersonId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid InitialStatusId = Guid.Parse("81000000-0000-0000-0000-000000000001");
    private static readonly Guid ClientTypeId = Guid.Parse("82000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Resolve_without_conversation_creates_profile_conversation_and_client_participant_once()
    {
        var fixture = CreateFixture();
        fixture.Profiles.GetByUserIdAsync(PersonId, fixture.Token)
            .Returns(Task.FromResult<IReadOnlyCollection<ChatUserProfile>>([]));
        var provider = fixture.CreateProvider();

        var result = await provider.ResolveAsync(
            PersonId,
            null,
            "message-001",
            fixture.Token);

        Assert.NotEqual(Guid.Empty, result.ConversationId);
        Assert.Equal("web", result.Channel);
        Assert.False(result.IsEscalated);
        await fixture.Profiles.Received(1).AddAsync(
            Arg.Is<ChatUserProfile>(profile => profile.UserId == PersonId),
            fixture.Token);
        await fixture.Conversations.Received(1).AddAsync(
            Arg.Is<ChatConversation>(conversation =>
                conversation.Id == result.ConversationId &&
                conversation.ConversationStatusId == InitialStatusId &&
                conversation.AiEnabled),
            fixture.Token);
        await fixture.Participants.Received(1).AddAsync(
            Arg.Is<ChatParticipant>(participant =>
                participant.ChatConversationId == result.ConversationId &&
                participant.ParticipantTypeId == ClientTypeId &&
                participant.ChatUserProfileId.HasValue),
            fixture.Token);
        await fixture.UnitOfWork.Received(1).SaveChangesAsync(fixture.Token);
    }

    [Fact]
    public async Task Resolve_without_conversation_reuses_existing_profile()
    {
        var fixture = CreateFixture();
        var profile = ChatUserProfile.Create(PersonId, "Samuel", null, null);
        fixture.Profiles.GetByUserIdAsync(PersonId, fixture.Token)
            .Returns(Task.FromResult<IReadOnlyCollection<ChatUserProfile>>([profile]));
        var provider = fixture.CreateProvider();

        var result = await provider.ResolveAsync(
            PersonId,
            null,
            "message-002",
            fixture.Token);

        await fixture.Profiles.DidNotReceive().AddAsync(
            Arg.Any<ChatUserProfile>(),
            Arg.Any<CancellationToken>());
        await fixture.Participants.Received(1).AddAsync(
            Arg.Is<ChatParticipant>(participant =>
                participant.ChatConversationId == result.ConversationId &&
                participant.ChatUserProfileId == profile.Id),
            fixture.Token);
        await fixture.UnitOfWork.Received(1).SaveChangesAsync(fixture.Token);
    }

    [Fact]
    public async Task Resolve_without_conversation_rejects_a_missing_initial_status()
    {
        var fixture = CreateFixture();
        fixture.UnitOfWork.ConversationStatusesRepository
            .GetByIdAsync(InitialStatusId, fixture.Token)
            .Returns(Task.FromResult<ConversationStatusEntity?>(null));
        var provider = fixture.CreateProvider();

        await Assert.ThrowsAsync<AgentConversationConfigurationException>(async () =>
            await provider.ResolveAsync(
                PersonId,
                null,
                "message-config",
                fixture.Token));

        await fixture.UnitOfWork.DidNotReceive().SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolve_existing_conversation_allows_an_owned_profile_participant()
    {
        var fixture = CreateFixture();
        var conversation = ChatConversation.Create(InitialStatusId);
        var firstProfile = ChatUserProfile.Create(PersonId, "Principal", null, null);
        var participatingProfile = ChatUserProfile.Create(PersonId, "Alterno", null, null);
        var participant = ChatParticipant.Create(
            conversation.Id,
            ClientTypeId,
            chatUserProfileId: participatingProfile.Id);
        fixture.ConfigureExistingConversation(
            conversation,
            [firstProfile, participatingProfile],
            [participant]);
        var provider = fixture.CreateProvider();

        var result = await provider.ResolveAsync(
            PersonId,
            conversation.Id,
            "message-003",
            fixture.Token);

        Assert.Equal(conversation.Id, result.ConversationId);
        Assert.False(result.IsEscalated);
        await fixture.UnitOfWork.DidNotReceive().SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolve_existing_conversation_rejects_a_missing_conversation()
    {
        var fixture = CreateFixture();
        var missingId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        fixture.Conversations.GetByIdAsync(missingId, fixture.Token)
            .Returns(Task.FromResult<ChatConversation?>(null));
        var provider = fixture.CreateProvider();

        await Assert.ThrowsAsync<AgentConversationNotFoundException>(async () =>
            await provider.ResolveAsync(
                PersonId,
                missingId,
                "message-004",
                fixture.Token));
    }

    [Fact]
    public async Task Resolve_existing_conversation_rejects_a_non_participant()
    {
        var fixture = CreateFixture();
        var conversation = ChatConversation.Create(InitialStatusId);
        var ownedProfile = ChatUserProfile.Create(PersonId, "Samuel", null, null);
        fixture.ConfigureExistingConversation(conversation, [ownedProfile], []);
        var provider = fixture.CreateProvider();

        await Assert.ThrowsAsync<AgentConversationForbiddenException>(async () =>
            await provider.ResolveAsync(
                PersonId,
                conversation.Id,
                "message-005",
                fixture.Token));
    }

    [Fact]
    public async Task Resolve_existing_conversation_rejects_a_deleted_authenticated_user()
    {
        var fixture = CreateFixture();
        var conversation = ChatConversation.Create(InitialStatusId);
        var profile = ChatUserProfile.Create(PersonId, "Samuel", null, null);
        var participant = ChatParticipant.Create(
            conversation.Id,
            ClientTypeId,
            chatUserProfileId: profile.Id);
        fixture.ConfigureExistingConversation(conversation, [profile], [participant]);
        fixture.Users.GetByIdAsync(PersonId, fixture.Token)
            .Returns(Task.FromResult<UserEntity?>(null));
        var provider = fixture.CreateProvider();

        await Assert.ThrowsAsync<AgentConversationForbiddenException>(async () =>
            await provider.ResolveAsync(
                PersonId,
                conversation.Id,
                "message-user-missing",
                fixture.Token));
    }

    [Fact]
    public async Task Resolve_existing_conversation_marks_an_unresolved_escalation()
    {
        var fixture = CreateFixture();
        var conversation = ChatConversation.Create(InitialStatusId);
        var profile = ChatUserProfile.Create(PersonId, "Samuel", null, null);
        var participant = ChatParticipant.Create(
            conversation.Id,
            ClientTypeId,
            chatUserProfileId: profile.Id);
        fixture.ConfigureExistingConversation(conversation, [profile], [participant]);
        fixture.EscalationReader.HasActiveAsync(conversation.Id, fixture.Token)
            .Returns(Task.FromResult(true));
        var provider = fixture.CreateProvider();

        var result = await provider.ResolveAsync(
            PersonId,
            conversation.Id,
            "message-006",
            fixture.Token);

        Assert.True(result.IsEscalated);
    }

    [Fact]
    public async Task Resolve_existing_conversation_ignores_a_resolved_escalation()
    {
        var fixture = CreateFixture();
        var conversation = ChatConversation.Create(InitialStatusId);
        var profile = ChatUserProfile.Create(PersonId, "Samuel", null, null);
        var participant = ChatParticipant.Create(
            conversation.Id,
            ClientTypeId,
            chatUserProfileId: profile.Id);
        fixture.ConfigureExistingConversation(conversation, [profile], [participant]);
        fixture.EscalationReader.HasActiveAsync(conversation.Id, fixture.Token)
            .Returns(Task.FromResult(false));
        var provider = fixture.CreateProvider();

        var result = await provider.ResolveAsync(
            PersonId,
            conversation.Id,
            "message-007",
            fixture.Token);

        Assert.False(result.IsEscalated);
    }

    private static Fixture CreateFixture()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var users = Substitute.For<IUsersRepository>();
        var profiles = Substitute.For<IChatUserProfileRepository>();
        var conversations = Substitute.For<IChatConversationRepository>();
        var participants = Substitute.For<IChatParticipantRepository>();
        var statuses = Substitute.For<IConversationStatusRepository>();
        var senderTypes = Substitute.For<ISenderTypeRepository>();
        var escalationReader = Substitute.For<IActiveConversationEscalationReader>();
        var token = new CancellationTokenSource().Token;

        unitOfWork.UsersRepository.Returns(users);
        unitOfWork.ChatUserProfilesRepository.Returns(profiles);
        unitOfWork.ChatConversationsRepository.Returns(conversations);
        unitOfWork.ChatParticipantsRepository.Returns(participants);
        unitOfWork.ConversationStatusesRepository.Returns(statuses);
        unitOfWork.SenderTypesRepository.Returns(senderTypes);
        users.GetByIdAsync(PersonId, token)
            .Returns(Task.FromResult<UserEntity?>(new UserEntity(
                "Samuel Calderón",
                "samuel@example.test",
                "hash",
                Guid.Parse("77777777-7777-7777-7777-777777777777"))));
        statuses.GetByIdAsync(InitialStatusId, token)
            .Returns(Task.FromResult<ConversationStatusEntity?>(new ConversationStatusEntity("Abierta")));
        senderTypes.GetByIdAsync(ClientTypeId, token)
            .Returns(Task.FromResult<SenderTypeEntity?>(new SenderTypeEntity("Cliente")));
        unitOfWork.SaveChangesAsync(token).Returns(Task.FromResult(3));

        return new Fixture(
            unitOfWork,
            users,
            profiles,
            conversations,
            participants,
            escalationReader,
            token);
    }

    private sealed record Defaults : IAgentConversationDefaults
    {
        public Guid InitialConversationStatusId => InitialStatusId;
        public Guid ClientParticipantTypeId => ClientTypeId;
    }

    private sealed record Fixture(
        IUnitOfWork UnitOfWork,
        IUsersRepository Users,
        IChatUserProfileRepository Profiles,
        IChatConversationRepository Conversations,
        IChatParticipantRepository Participants,
        IActiveConversationEscalationReader EscalationReader,
        CancellationToken Token)
    {
        public PersistentConversationContextProvider CreateProvider() =>
            new(UnitOfWork, new Defaults(), EscalationReader);

        public void ConfigureExistingConversation(
            ChatConversation conversation,
            IReadOnlyCollection<ChatUserProfile> profiles,
            IReadOnlyCollection<ChatParticipant> participants)
        {
            Conversations.GetByIdAsync(conversation.Id, Token)
                .Returns(Task.FromResult<ChatConversation?>(conversation));
            Profiles.GetByUserIdAsync(PersonId, Token)
                .Returns(Task.FromResult(profiles));
            Participants.GetAllByConversationIdAsync(conversation.Id, Token)
                .Returns(Task.FromResult(participants));
            EscalationReader.HasActiveAsync(conversation.Id, Token)
                .Returns(Task.FromResult(false));
        }
    }
}
