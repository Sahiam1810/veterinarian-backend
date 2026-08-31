using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatAiRunEntity = Domain.ChatAiRuns.Entities.ChatAiRun;

namespace Application.ChatAiRuns.UseCase;

public sealed record CreateChatAiRunCommand(
    Guid ChatConversationId,
    Guid ChatMessageId,
    Guid AiModelId,
    Guid AiRunStatusId) : IRequest<ChatAiRunEntity>;

public sealed class CreateChatAiRunCommandHandler
    : IRequestHandler<CreateChatAiRunCommand, ChatAiRunEntity>
{
    private readonly IUnitOfWork _uow;

    public CreateChatAiRunCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatAiRunEntity> Handle(
        CreateChatAiRunCommand request,
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

        var message = await _uow.ChatMessagesRepository.GetByIdAsync(
            request.ChatMessageId,
            cancellationToken);
        if (message is null)
        {
            throw new NotFoundException(
                $"No se encontró el mensaje '{request.ChatMessageId}'.");
        }

        var aiModel = await _uow.AiModelsRepository.GetByIdAsync(
            request.AiModelId,
            cancellationToken);
        if (aiModel is null)
        {
            throw new NotFoundException(
                $"No se encontró el modelo de IA '{request.AiModelId}'.");
        }

        var aiRunStatus = await _uow.AiRunStatusesRepository.GetByIdAsync(
            request.AiRunStatusId,
            cancellationToken);
        if (aiRunStatus is null)
        {
            throw new NotFoundException(
                $"No se encontró el estado de ejecución '{request.AiRunStatusId}'.");
        }

        if (message.ChatConversationId != request.ChatConversationId)
        {
            throw new ArgumentException(
                "El mensaje no pertenece a la conversación indicada.");
        }

        var chatAiRun = ChatAiRunEntity.Create(
            request.ChatConversationId,
            request.ChatMessageId,
            request.AiModelId,
            request.AiRunStatusId);

        await _uow.ChatAiRunsRepository.AddAsync(chatAiRun, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return chatAiRun;
    }
}
