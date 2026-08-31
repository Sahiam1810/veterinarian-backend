using Application.Common.Abstractions;
using MediatR;
using ChatEscalationAssignmentEntity = Domain.ChatEscalationAssignments.Entities.ChatEscalationAssignment;

namespace Application.ChatEscalationAssignments.UseCase;

public sealed record GetChatEscalationAssignmentsByAgentHumanIdQuery(Guid AgentHumanId)
    : IRequest<IReadOnlyCollection<ChatEscalationAssignmentEntity>>;

public sealed class GetChatEscalationAssignmentsByAgentHumanIdQueryHandler
    : IRequestHandler<GetChatEscalationAssignmentsByAgentHumanIdQuery, IReadOnlyCollection<ChatEscalationAssignmentEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetChatEscalationAssignmentsByAgentHumanIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatEscalationAssignmentEntity>> Handle(
        GetChatEscalationAssignmentsByAgentHumanIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatEscalationAssignmentsRepository.GetByAgentHumanIdAsync(
            request.AgentHumanId,
            cancellationToken);
}
