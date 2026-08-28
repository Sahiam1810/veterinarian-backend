using ChatConversationAiSettingEntity = Domain.ChatConversationAiSettings.Entities.ChatConversationAiSetting;

namespace Application.ChatConversationAiSettings.Abstraction;

public interface IChatConversationAiSettingRepository
{
    Task<IReadOnlyCollection<ChatConversationAiSettingEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ChatConversationAiSettingEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ChatConversationAiSettingEntity?> GetByConversationIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ChatConversationAiSettingEntity setting,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ChatConversationAiSettingEntity setting,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ChatConversationAiSettingEntity setting,
        CancellationToken cancellationToken = default);
}
