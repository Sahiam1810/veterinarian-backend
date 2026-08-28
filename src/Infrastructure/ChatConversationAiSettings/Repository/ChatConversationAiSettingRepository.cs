using Application.ChatConversationAiSettings.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChatConversationAiSettingEntity = Domain.ChatConversationAiSettings.Entities.ChatConversationAiSetting;

namespace Infrastructure.ChatConversationAiSettings.Repository;

public sealed class ChatConversationAiSettingRepository : IChatConversationAiSettingRepository
{
    private readonly VeterinaryDbContext _context;

    public ChatConversationAiSettingRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ChatConversationAiSettingEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatConversationAiSettingEntity>()
            .AsNoTracking()
            .OrderBy(setting => setting.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<ChatConversationAiSettingEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatConversationAiSettingEntity>()
            .FirstOrDefaultAsync(setting => setting.Id == id, cancellationToken);

    public Task<ChatConversationAiSettingEntity?> GetByConversationIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatConversationAiSettingEntity>()
            .AsNoTracking()
            .Where(setting => setting.ConversationId == conversationId)
            .OrderByDescending(setting => setting.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(
        ChatConversationAiSettingEntity setting,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatConversationAiSettingEntity>().AddAsync(setting, cancellationToken);

    public Task UpdateAsync(
        ChatConversationAiSettingEntity setting,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ChatConversationAiSettingEntity>().Update(setting);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        ChatConversationAiSettingEntity setting,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ChatConversationAiSettingEntity>().Remove(setting);
        return Task.CompletedTask;
    }
}
