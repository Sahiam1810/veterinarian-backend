using Application.Common.Abstractions;
using MediatR;
using ChatParticipantEntity = Domain.ChatParticipants.Entities.ChatParticipant;

namespace Application.ChatParticipants.UseCase;

public sealed record GetChatParticipantsByConversationIdQuery(Guid ChatConversationId)
    : IRequest<IReadOnlyCollection<ChatParticipantEntity>>;

public sealed class GetChatParticipantsByConversationIdQueryHandler
    : IRequestHandler<GetChatParticipantsByConversationIdQuery, IReadOnlyCollection<ChatParticipantEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetChatParticipantsByConversationIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatParticipantEntity>> Handle(
        GetChatParticipantsByConversationIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatParticipantsRepository.GetAllByConversationIdAsync(
            request.ChatConversationId,
            cancellationToken);
}
