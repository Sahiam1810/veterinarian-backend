using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatConversationAiSettingEntity = Domain.ChatConversationAiSettings.Entities.ChatConversationAiSetting;

namespace Application.ChatConversationAiSettings.UseCase;

public sealed record UpdateChatConversationAiSettingCommand(
    Guid Id,
    bool AiEnabled,
    Guid? DefaultModelId) : IRequest<ChatConversationAiSettingEntity>;

public sealed class UpdateChatConversationAiSettingCommandHandler
    : IRequestHandler<UpdateChatConversationAiSettingCommand, ChatConversationAiSettingEntity>
{
    private readonly IUnitOfWork _uow;

    public UpdateChatConversationAiSettingCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatConversationAiSettingEntity> Handle(
        UpdateChatConversationAiSettingCommand request,
        CancellationToken cancellationToken)
    {
        var setting = await _uow.ChatConversationAiSettingsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la configuración de IA '{request.Id}'.");

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

        setting.Update(request.AiEnabled, request.DefaultModelId);

        await _uow.ChatConversationAiSettingsRepository.UpdateAsync(setting, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return setting;
    }
}
