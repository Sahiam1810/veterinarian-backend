using Application.Common.Abstractions;
using MediatR;
using ChatConversationEntity = Domain.ChatConversations.Entities.ChatConversation;

namespace Application.ChatConversations.UseCase;

public sealed record GetAllChatConversationsQuery() : IRequest<IReadOnlyCollection<ChatConversationEntity>>;

public sealed class GetAllChatConversationsQueryHandler
    : IRequestHandler<GetAllChatConversationsQuery, IReadOnlyCollection<ChatConversationEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllChatConversationsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatConversationEntity>> Handle(
        GetAllChatConversationsQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatConversationsRepository.GetAllAsync(cancellationToken);
}
