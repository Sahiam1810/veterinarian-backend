using Application.Common.Abstractions;
using MediatR;
using ChatConversationEntity = Domain.ChatConversations.Entities.ChatConversation;

namespace Application.ChatConversations.UseCase;

public sealed record GetChatConversationByIdQuery(Guid Id) : IRequest<ChatConversationEntity?>;

public sealed class GetChatConversationByIdQueryHandler
    : IRequestHandler<GetChatConversationByIdQuery, ChatConversationEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetChatConversationByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<ChatConversationEntity?> Handle(
        GetChatConversationByIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatConversationsRepository.GetByIdAsync(request.Id, cancellationToken);
}
