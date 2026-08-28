using Application.Common.Abstractions;
using MediatR;
using ChatConversationAssignmentEntity = Domain.ChatConversationAssignments.Entities.ChatConversationAssignment;

namespace Application.ChatConversationAssignments.UseCase;

public sealed record GetChatConversationAssignmentByConversationIdQuery(Guid ChatConversationId)
    : IRequest<ChatConversationAssignmentEntity?>;

public sealed class GetChatConversationAssignmentByConversationIdQueryHandler
    : IRequestHandler<GetChatConversationAssignmentByConversationIdQuery, ChatConversationAssignmentEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetChatConversationAssignmentByConversationIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<ChatConversationAssignmentEntity?> Handle(
        GetChatConversationAssignmentByConversationIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatConversationAssignmentsRepository.GetByConversationIdAsync(
            request.ChatConversationId,
            cancellationToken);
}
