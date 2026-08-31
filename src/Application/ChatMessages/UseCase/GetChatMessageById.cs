using Application.Common.Abstractions;
using MediatR;
using ChatMessageEntity = Domain.ChatMessages.Entities.ChatMessage;

namespace Application.ChatMessages.UseCase;

public sealed record GetChatMessageByIdQuery(Guid Id)
    : IRequest<ChatMessageEntity?>;

public sealed class GetChatMessageByIdQueryHandler
    : IRequestHandler<GetChatMessageByIdQuery, ChatMessageEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetChatMessageByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<ChatMessageEntity?> Handle(
        GetChatMessageByIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatMessagesRepository.GetByIdAsync(request.Id, cancellationToken);
}
