using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.ChatConversationAiSettings.UseCase;

public sealed record DeleteChatConversationAiSettingCommand(Guid Id) : IRequest;

public sealed class DeleteChatConversationAiSettingCommandHandler
    : IRequestHandler<DeleteChatConversationAiSettingCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteChatConversationAiSettingCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        DeleteChatConversationAiSettingCommand request,
        CancellationToken cancellationToken)
    {
        var setting = await _uow.ChatConversationAiSettingsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la configuración de IA '{request.Id}'.");

        await _uow.ChatConversationAiSettingsRepository.DeleteAsync(setting, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
