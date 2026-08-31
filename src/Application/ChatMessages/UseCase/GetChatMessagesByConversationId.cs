using Application.Common.Abstractions;
using MediatR;
using ChatMessageEntity = Domain.ChatMessages.Entities.ChatMessage;

namespace Application.ChatMessages.UseCase;

public sealed record GetChatMessagesByConversationIdQuery(Guid ChatConversationId)
    : IRequest<IReadOnlyCollection<ChatMessageEntity>>;

public sealed class GetChatMessagesByConversationIdQueryHandler
    : IRequestHandler<GetChatMessagesByConversationIdQuery, IReadOnlyCollection<ChatMessageEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetChatMessagesByConversationIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatMessageEntity>> Handle(
        GetChatMessagesByConversationIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatMessagesRepository.GetAllByConversationIdAsync(
            request.ChatConversationId,
            cancellationToken);
}
