using Application.Common.Abstractions;
using MediatR;
using ChatEscalationAssignmentEntity = Domain.ChatEscalationAssignments.Entities.ChatEscalationAssignment;

namespace Application.ChatEscalationAssignments.UseCase;

public sealed record GetAllChatEscalationAssignmentsQuery
    : IRequest<IReadOnlyCollection<ChatEscalationAssignmentEntity>>;

public sealed class GetAllChatEscalationAssignmentsQueryHandler
    : IRequestHandler<GetAllChatEscalationAssignmentsQuery, IReadOnlyCollection<ChatEscalationAssignmentEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllChatEscalationAssignmentsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatEscalationAssignmentEntity>> Handle(
        GetAllChatEscalationAssignmentsQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatEscalationAssignmentsRepository.GetAllAsync(cancellationToken);
}
