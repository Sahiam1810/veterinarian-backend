using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatConversationAiSettingEntity = Domain.ChatConversationAiSettings.Entities.ChatConversationAiSetting;

namespace Application.ChatConversationAiSettings.UseCase;

public sealed record CreateChatConversationAiSettingCommand(
    Guid ConversationId,
    bool AiEnabled,
    Guid? DefaultModelId) : IRequest<ChatConversationAiSettingEntity>;

public sealed class CreateChatConversationAiSettingCommandHandler
    : IRequestHandler<CreateChatConversationAiSettingCommand, ChatConversationAiSettingEntity>
{
    private readonly IUnitOfWork _uow;

    public CreateChatConversationAiSettingCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatConversationAiSettingEntity> Handle(
        CreateChatConversationAiSettingCommand request,
        CancellationToken cancellationToken)
    {
        var conversation = await _uow.ChatConversationsRepository.GetByIdAsync(
            request.ConversationId,
            cancellationToken);
        if (conversation is null)
        {
            throw new NotFoundException(
                $"No se encontró la conversación '{request.ConversationId}'.");
        }

        if (request.DefaultModelId.HasValue)
        {
            var model = await _uow.AiModelsRepository.GetByIdAsync(
                request.DefaultModelId.Value,
                cancellationToken);
            if (model is null)
            {
                throw new NotFoundException(
                    $"No se encontró el modelo de IA '{request.DefaultModelId.Value}'.");
            }
        }

        var setting = ChatConversationAiSettingEntity.Create(
            request.ConversationId,
            request.AiEnabled,
            request.DefaultModelId);

        await _uow.ChatConversationAiSettingsRepository.AddAsync(setting, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return setting;
    }
}
