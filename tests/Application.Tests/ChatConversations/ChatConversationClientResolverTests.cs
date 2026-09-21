using Application.Agent.Abstractions;
using Application.ChatConversations.Abstraction;
using Application.ChatConversations.Enrichment;
using Application.Common.Abstractions;
using Application.Tests.Common;
using Domain.ChatParticipants.Entities;
using Domain.Clients.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.ChatConversations;

public sealed class ChatConversationClientResolverTests
{
    private static readonly Guid ConversationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ClientTypeId = Guid.Parse("82000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Resolve_returns_name_and_phone_from_the_client_participant()
    {
        var client = TestClients.Create("1234567890", null, phoneNumber: "3001234567", fullName: "Ana Pérez");
        var participant = ChatParticipant.Create(ConversationId, ClientTypeId, clientId: client.Id);
        var resolver = CreateResolver([participant], client);

        var info = await resolver.ResolveAsync(ConversationId, CancellationToken.None);

        Assert.Equal(client.Id, info.ClientId);
        Assert.Equal("Ana Pérez", info.ClientName);
        Assert.Equal("3001234567", info.ClientPhone);
    }

    [Fact]
    public async Task Resolve_returns_empty_when_the_client_no_longer_exists()
    {
        var participant = ChatParticipant.Create(ConversationId, ClientTypeId, clientId: Guid.NewGuid());
        var resolver = CreateResolver([participant], client: null);

        var info = await resolver.ResolveAsync(ConversationId, CancellationToken.None);

        Assert.Equal(ChatConversationClientInfo.Empty, info);
    }

    [Fact]
    public async Task Resolve_returns_empty_when_the_participant_has_no_client()
    {
        var participant = ChatParticipant.Create(ConversationId, ClientTypeId, agentHumanId: Guid.NewGuid());
        var resolver = CreateResolver([participant], client: null);

        var info = await resolver.ResolveAsync(ConversationId, CancellationToken.None);

        Assert.Equal(ChatConversationClientInfo.Empty, info);
    }

    private static ChatConversationClientResolver CreateResolver(
        IReadOnlyCollection<ChatParticipant> participants,
        ClientEntity? client)
    {
        var uow = Substitute.For<IUnitOfWork>();
        uow.ChatParticipantsRepository
            .GetAllByConversationIdAsync(ConversationId, Arg.Any<CancellationToken>())
            .Returns(participants);
        uow.ClientsRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(client);

        var defaults = Substitute.For<IAgentConversationDefaults>();
        defaults.ClientParticipantTypeId.Returns(ClientTypeId);

        return new ChatConversationClientResolver(uow, defaults);
    }
}
