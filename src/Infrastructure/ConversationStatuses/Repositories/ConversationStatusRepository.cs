using Application.ConversationStatuses.Abstraction;
using Domain.ConversationStatuses.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ConversationStatuses.Repositories;

// Repositorio EF Core de estados de conversación.
public sealed class ConversationStatusRepository(VeterinaryDbContext context) : IConversationStatusRepository
{
    public async Task<IReadOnlyCollection<ConversationStatusEntity>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Set<ConversationStatusEntity>()
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<ConversationStatusEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Set<ConversationStatusEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(ConversationStatusEntity conversationStatus, CancellationToken cancellationToken) =>
        await context.Set<ConversationStatusEntity>().AddAsync(conversationStatus, cancellationToken);

    public Task UpdateAsync(ConversationStatusEntity conversationStatus, CancellationToken cancellationToken)
    {
        context.Set<ConversationStatusEntity>().Update(conversationStatus);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ConversationStatusEntity conversationStatus, CancellationToken cancellationToken)
    {
        context.Set<ConversationStatusEntity>().Remove(conversationStatus);
        return Task.CompletedTask;
    }
}
