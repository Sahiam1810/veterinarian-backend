using Application.Common.Abstractions;
using MediatR;
using ChatConversationAiSettingEntity = Domain.ChatConversationAiSettings.Entities.ChatConversationAiSetting;

namespace Application.ChatConversationAiSettings.UseCase;

public sealed record GetChatConversationAiSettingByIdQuery(Guid Id)
    : IRequest<ChatConversationAiSettingEntity?>;

public sealed class GetChatConversationAiSettingByIdQueryHandler
    : IRequestHandler<GetChatConversationAiSettingByIdQuery, ChatConversationAiSettingEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetChatConversationAiSettingByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<ChatConversationAiSettingEntity?> Handle(
        GetChatConversationAiSettingByIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatConversationAiSettingsRepository.GetByIdAsync(request.Id, cancellationToken);
}
