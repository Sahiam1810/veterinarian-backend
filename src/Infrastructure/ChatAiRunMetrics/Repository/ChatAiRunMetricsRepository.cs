using Application.ChatAiRunMetrics.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ChatAiRunMetricsEntity = Domain.ChatAiRunMetrics.Entities.ChatAiRunMetrics;

namespace Infrastructure.ChatAiRunMetrics.Repository;

public sealed class ChatAiRunMetricsRepository : IChatAiRunMetricsRepository
{
    private readonly VeterinaryDbContext _context;

    public ChatAiRunMetricsRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        ChatAiRunMetricsEntity chatAiRunMetrics,
        CancellationToken cancellationToken = default)
        => await _context.Set<ChatAiRunMetricsEntity>().AddAsync(chatAiRunMetrics, cancellationToken);

    public Task<ChatAiRunMetricsEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatAiRunMetricsEntity>()
            .FirstOrDefaultAsync(metrics => metrics.Id == id, cancellationToken);

    public Task<ChatAiRunMetricsEntity?> GetByChatAiRunIdAsync(
        Guid chatAiRunId,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatAiRunMetricsEntity>()
            .FirstOrDefaultAsync(metrics => metrics.ChatAiRunId == chatAiRunId, cancellationToken);

    public Task<bool> ExistsByChatAiRunIdAsync(
        Guid chatAiRunId,
        CancellationToken cancellationToken = default)
        => _context.Set<ChatAiRunMetricsEntity>()
            .AnyAsync(metrics => metrics.ChatAiRunId == chatAiRunId, cancellationToken);
}
