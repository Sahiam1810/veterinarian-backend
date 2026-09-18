using Application.ChatConversations.Abstraction;
using Application.Common.Abstractions;
using MediatR;
using ChatConversationEntity = Domain.ChatConversations.Entities.ChatConversation;

namespace Application.ChatConversations.UseCase;

// Ticket B7: conversación + datos del cliente vinculado (si tiene uno), para
// que GET /api/chat/conversations no siempre devuelva "Cliente sin nombre".
public sealed record ChatConversationWithClient(
    ChatConversationEntity Conversation,
    string? ClientName,
    string? ClientPhone);

public sealed record GetAllChatConversationsQuery
    : IRequest<IReadOnlyCollection<ChatConversationWithClient>>;

public sealed class GetAllChatConversationsQueryHandler(
    IUnitOfWork uow,
    IChatConversationClientResolver clientResolver)
    : IRequestHandler<GetAllChatConversationsQuery, IReadOnlyCollection<ChatConversationWithClient>>
{
    public async Task<IReadOnlyCollection<ChatConversationWithClient>> Handle(
        GetAllChatConversationsQuery request,
        CancellationToken cancellationToken)
    {
        var conversations = await uow.ChatConversationsRepository.GetAllAsync(cancellationToken);

        var results = new List<ChatConversationWithClient>(conversations.Count);
        foreach (var conversation in conversations)
        {
            var clientInfo = await clientResolver.ResolveAsync(conversation.Id, cancellationToken);
            results.Add(new ChatConversationWithClient(
                conversation, clientInfo.ClientName, clientInfo.ClientPhone));
        }

        return results;
    }
}
