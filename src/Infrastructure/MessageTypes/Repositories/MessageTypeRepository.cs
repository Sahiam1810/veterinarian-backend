using Application.MessageTypes.Abstraction;
using Domain.MessageTypes.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.MessageTypes.Repositories;

public sealed class MessageTypeRepository(VeterinaryDbContext context) : IMessageTypeRepository
{
    public async Task<IReadOnlyCollection<MessageTypeEntity>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Set<MessageTypeEntity>()
            .AsNoTracking()
            .OrderBy(x => x.Name.Value)
            .ToListAsync(cancellationToken);

    public Task<MessageTypeEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Set<MessageTypeEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(MessageTypeEntity messageType, CancellationToken cancellationToken) =>
        await context.Set<MessageTypeEntity>().AddAsync(messageType, cancellationToken);

    public Task UpdateAsync(MessageTypeEntity messageType, CancellationToken cancellationToken)
    {
        context.Set<MessageTypeEntity>().Update(messageType);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(MessageTypeEntity messageType, CancellationToken cancellationToken)
    {
        context.Set<MessageTypeEntity>().Remove(messageType);
        return Task.CompletedTask;
    }
}
