using Application.Common.Abstractions;
using MediatR;
using ChatConversationAssignmentEntity = Domain.ChatConversationAssignments.Entities.ChatConversationAssignment;

namespace Application.ChatConversationAssignments.UseCase;

public sealed record GetChatConversationAssignmentsByAgentHumanIdQuery(Guid AgentHumanId)
    : IRequest<IReadOnlyCollection<ChatConversationAssignmentEntity>>;

public sealed class GetChatConversationAssignmentsByAgentHumanIdQueryHandler
    : IRequestHandler<GetChatConversationAssignmentsByAgentHumanIdQuery, IReadOnlyCollection<ChatConversationAssignmentEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetChatConversationAssignmentsByAgentHumanIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatConversationAssignmentEntity>> Handle(
        GetChatConversationAssignmentsByAgentHumanIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatConversationAssignmentsRepository.GetByAgentHumanIdAsync(
            request.AgentHumanId,
            cancellationToken);
}
