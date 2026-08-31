using Application.Common.Abstractions;
using MediatR;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;

namespace Application.ChatEscalations.UseCase;

public sealed record GetChatEscalationsByConversationIdQuery(Guid ChatConversationId)
    : IRequest<IReadOnlyCollection<ChatEscalationEntity>>;

public sealed class GetChatEscalationsByConversationIdQueryHandler
    : IRequestHandler<GetChatEscalationsByConversationIdQuery, IReadOnlyCollection<ChatEscalationEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetChatEscalationsByConversationIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatEscalationEntity>> Handle(
        GetChatEscalationsByConversationIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatEscalationsRepository.GetByConversationIdAsync(
            request.ChatConversationId,
            cancellationToken);
}
