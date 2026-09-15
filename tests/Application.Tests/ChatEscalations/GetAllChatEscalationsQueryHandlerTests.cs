using Application.ChatEscalations.UseCase;
using Application.Common.Abstractions;
using Domain.ChatConversations.Entities;
using Domain.Priorities.Entities;
using NSubstitute;
using Xunit;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;

namespace Application.Tests.ChatEscalations;

public sealed class GetAllChatEscalationsQueryHandlerTests
{
    private static readonly Guid EscalationStatusId = Guid.Parse("85000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Handle_includes_the_priority_of_the_linked_conversation()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var priorityId = Guid.NewGuid();
        var conversation = ChatConversation.Create(Guid.NewGuid(), priorityId);
        var escalation = ChatEscalationEntity.Create(conversation.Id, EscalationStatusId, false, "asesor");

        uow.ChatEscalationsRepository.GetAllAsync(default)
            .Returns((IReadOnlyCollection<ChatEscalationEntity>)new[] { escalation });
        uow.ChatConversationsRepository.GetByIdAsync(conversation.Id, default)
            .Returns(conversation);
        uow.PrioritiesRepository.GetByIdAsync(priorityId, default)
            .Returns(new PriorityEntity("Alta"));

        var handler = new GetAllChatEscalationsQueryHandler(uow);
        var results = await handler.Handle(new GetAllChatEscalationsQuery(), CancellationToken.None);

        var item = Assert.Single(results);
        Assert.Equal(priorityId, item.PriorityId);
        Assert.Equal("Alta", item.Priority);
    }

    [Fact]
    public async Task Handle_leaves_priority_null_when_the_conversation_has_none()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var conversation = ChatConversation.Create(Guid.NewGuid());
        var escalation = ChatEscalationEntity.Create(conversation.Id, EscalationStatusId, false, "asesor");

        uow.ChatEscalationsRepository.GetAllAsync(default)
            .Returns((IReadOnlyCollection<ChatEscalationEntity>)new[] { escalation });
        uow.ChatConversationsRepository.GetByIdAsync(conversation.Id, default)
            .Returns(conversation);

        var handler = new GetAllChatEscalationsQueryHandler(uow);
        var results = await handler.Handle(new GetAllChatEscalationsQuery(), CancellationToken.None);

        var item = Assert.Single(results);
        Assert.Null(item.PriorityId);
        Assert.Null(item.Priority);
    }

    [Fact]
    public async Task Handle_does_not_fail_when_the_linked_conversation_is_missing()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var missingConversationId = Guid.NewGuid();
        var escalation = ChatEscalationEntity.Create(missingConversationId, EscalationStatusId, false, "asesor");

        uow.ChatEscalationsRepository.GetAllAsync(default)
            .Returns((IReadOnlyCollection<ChatEscalationEntity>)new[] { escalation });
        uow.ChatConversationsRepository.GetByIdAsync(missingConversationId, default)
            .Returns((ChatConversation?)null);

        var handler = new GetAllChatEscalationsQueryHandler(uow);
        var results = await handler.Handle(new GetAllChatEscalationsQuery(), CancellationToken.None);

        var item = Assert.Single(results);
        Assert.Null(item.PriorityId);
        Assert.Null(item.Priority);
    }
}
