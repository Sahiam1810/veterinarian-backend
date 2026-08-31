using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatMessageEntity = Domain.ChatMessages.Entities.ChatMessage;

namespace Application.ChatMessages.UseCase;

public sealed record CreateChatMessageCommand(
    Guid ChatConversationId,
    Guid ChatParticipantId,
    Guid SenderTypesId,
    Guid MessageTypeId,
    string Content,
    string? Metadata) : IRequest<ChatMessageEntity>;

public sealed class CreateChatMessageCommandHandler
    : IRequestHandler<CreateChatMessageCommand, ChatMessageEntity>
{
    private readonly IUnitOfWork _uow;

    public CreateChatMessageCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatMessageEntity> Handle(
        CreateChatMessageCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await _uow.ChatConversationsRepository.GetByIdAsync(
            request.ChatConversationId,
            cancellationToken);
        if (conversation is null)
        {
            throw new NotFoundException(
                $"No se encontró la conversación '{request.ChatConversationId}'.");
        }

        var participant = await _uow.ChatParticipantsRepository.GetByIdAsync(
            request.ChatParticipantId,
            cancellationToken);
        if (participant is null)
        {
            throw new NotFoundException(
                $"No se encontró el participante '{request.ChatParticipantId}'.");
        }

        var senderType = await _uow.SenderTypesRepository.GetByIdAsync(
            request.SenderTypesId,
            cancellationToken);
        if (senderType is null)
        {
            throw new NotFoundException(
                $"No se encontró el tipo de remitente '{request.SenderTypesId}'.");
        }

        var messageType = await _uow.MessageTypesRepository.GetByIdAsync(
            request.MessageTypeId,
            cancellationToken);
        if (messageType is null)
        {
            throw new NotFoundException(
                $"No se encontró el tipo de mensaje '{request.MessageTypeId}'.");
        }

        if (participant.ChatConversationId != request.ChatConversationId)
        {
            throw new ArgumentException(
                "El participante no pertenece a la conversación indicada.");
        }

        if (participant.ParticipantTypeId != request.SenderTypesId)
        {
            throw new ArgumentException(
                "El tipo de remitente no coincide con el tipo de participante.");
        }

        var message = ChatMessageEntity.Create(
            request.ChatConversationId,
            request.SenderTypesId,
            request.MessageTypeId,
            request.ChatParticipantId,
            request.Content,
            request.Metadata);

        conversation.UpdateLastMessageAt(message.CreatedAt);

        await _uow.ChatMessagesRepository.AddAsync(message, cancellationToken);
        await _uow.ChatConversationsRepository.UpdateAsync(conversation, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return message;
    }
}
