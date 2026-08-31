using Application.Common.Abstractions;
using MediatR;
using ChatEscalationAssignmentEntity = Domain.ChatEscalationAssignments.Entities.ChatEscalationAssignment;

namespace Application.ChatEscalationAssignments.UseCase;

public sealed record GetChatEscalationAssignmentsByChatEscalationIdQuery(Guid ChatEscalationId)
    : IRequest<IReadOnlyCollection<ChatEscalationAssignmentEntity>>;

public sealed class GetChatEscalationAssignmentsByChatEscalationIdQueryHandler
    : IRequestHandler<GetChatEscalationAssignmentsByChatEscalationIdQuery, IReadOnlyCollection<ChatEscalationAssignmentEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetChatEscalationAssignmentsByChatEscalationIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatEscalationAssignmentEntity>> Handle(
        GetChatEscalationAssignmentsByChatEscalationIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatEscalationAssignmentsRepository.GetByChatEscalationIdAsync(
            request.ChatEscalationId,
            cancellationToken);
}
