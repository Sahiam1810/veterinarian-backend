using Application.ChatEscalations.UseCase;
using Application.Common.Abstractions;
using NSubstitute;
using Xunit;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;

namespace Application.Tests.ChatEscalations;

public sealed class GetAllChatEscalationsQueryHandlerTests
{
    private static readonly Guid EscalationStatusId = Guid.Parse("85000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Handle_returns_all_escalations_from_repository()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var escalation = ChatEscalationEntity.Create(Guid.NewGuid(), EscalationStatusId, false, "asesor");

        uow.ChatEscalationsRepository.GetAllAsync(default)
            .Returns((IReadOnlyCollection<ChatEscalationEntity>)new[] { escalation });

        var handler = new GetAllChatEscalationsQueryHandler(uow);
        var results = await handler.Handle(new GetAllChatEscalationsQuery(), CancellationToken.None);

        var item = Assert.Single(results);
        Assert.Equal(escalation.Id, item.Id);
    }
}
