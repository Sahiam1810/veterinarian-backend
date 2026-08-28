using Application.Common.Abstractions;
using MediatR;
using ChatConversationAssignmentEntity = Domain.ChatConversationAssignments.Entities.ChatConversationAssignment;

namespace Application.ChatConversationAssignments.UseCase;

public sealed record GetAllChatConversationAssignmentsQuery
    : IRequest<IReadOnlyCollection<ChatConversationAssignmentEntity>>;

public sealed class GetAllChatConversationAssignmentsQueryHandler
    : IRequestHandler<GetAllChatConversationAssignmentsQuery, IReadOnlyCollection<ChatConversationAssignmentEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllChatConversationAssignmentsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatConversationAssignmentEntity>> Handle(
        GetAllChatConversationAssignmentsQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatConversationAssignmentsRepository.GetAllAsync(cancellationToken);
}
