using Application.Common.Abstractions;
using MediatR;
using ChatEscalationAssignmentEntity = Domain.ChatEscalationAssignments.Entities.ChatEscalationAssignment;

namespace Application.ChatEscalationAssignments.UseCase;

public sealed record GetChatEscalationAssignmentByIdQuery(Guid Id)
    : IRequest<ChatEscalationAssignmentEntity?>;

public sealed class GetChatEscalationAssignmentByIdQueryHandler
    : IRequestHandler<GetChatEscalationAssignmentByIdQuery, ChatEscalationAssignmentEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetChatEscalationAssignmentByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<ChatEscalationAssignmentEntity?> Handle(
        GetChatEscalationAssignmentByIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatEscalationAssignmentsRepository.GetByIdAsync(request.Id, cancellationToken);
}
