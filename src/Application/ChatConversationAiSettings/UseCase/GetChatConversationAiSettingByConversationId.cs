using Application.Common.Abstractions;
using MediatR;
using ChatConversationAiSettingEntity = Domain.ChatConversationAiSettings.Entities.ChatConversationAiSetting;

namespace Application.ChatConversationAiSettings.UseCase;

public sealed record GetChatConversationAiSettingByConversationIdQuery(Guid ConversationId)
    : IRequest<ChatConversationAiSettingEntity?>;

public sealed class GetChatConversationAiSettingByConversationIdQueryHandler
    : IRequestHandler<GetChatConversationAiSettingByConversationIdQuery, ChatConversationAiSettingEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetChatConversationAiSettingByConversationIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<ChatConversationAiSettingEntity?> Handle(
        GetChatConversationAiSettingByConversationIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatConversationAiSettingsRepository.GetByConversationIdAsync(
            request.ConversationId,
            cancellationToken);
}
