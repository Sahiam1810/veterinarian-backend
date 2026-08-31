using Application.SenderTypes.Abstraction;
using Domain.SenderTypes.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SenderTypes.Repositories;

public sealed class SenderTypeRepository(VeterinaryDbContext context) : ISenderTypeRepository
{
    public async Task<IReadOnlyCollection<SenderTypeEntity>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Set<SenderTypeEntity>().AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
    public Task<SenderTypeEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => context.Set<SenderTypeEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task AddAsync(SenderTypeEntity senderType, CancellationToken cancellationToken) => await context.Set<SenderTypeEntity>().AddAsync(senderType, cancellationToken);
    public Task UpdateAsync(SenderTypeEntity senderType, CancellationToken cancellationToken) { context.Set<SenderTypeEntity>().Update(senderType); return Task.CompletedTask; }
    public Task DeleteAsync(SenderTypeEntity senderType, CancellationToken cancellationToken) { context.Set<SenderTypeEntity>().Remove(senderType); return Task.CompletedTask; }
}
