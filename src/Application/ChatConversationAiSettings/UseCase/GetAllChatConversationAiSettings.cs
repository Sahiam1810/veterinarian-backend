using Application.Common.Abstractions;
using MediatR;
using ChatConversationAiSettingEntity = Domain.ChatConversationAiSettings.Entities.ChatConversationAiSetting;

namespace Application.ChatConversationAiSettings.UseCase;

public sealed record GetAllChatConversationAiSettingsQuery
    : IRequest<IReadOnlyCollection<ChatConversationAiSettingEntity>>;

public sealed class GetAllChatConversationAiSettingsQueryHandler
    : IRequestHandler<GetAllChatConversationAiSettingsQuery, IReadOnlyCollection<ChatConversationAiSettingEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllChatConversationAiSettingsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatConversationAiSettingEntity>> Handle(
        GetAllChatConversationAiSettingsQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatConversationAiSettingsRepository.GetAllAsync(cancellationToken);
}
