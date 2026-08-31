using Application.AiRunStatuses.Abstraction;
using Domain.AiRunStatuses.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.AiRunStatuses.Repositories;

public sealed class AiRunStatusRepository(VeterinaryDbContext context) : IAiRunStatusRepository
{
    public async Task<IReadOnlyCollection<AiRunStatusEntity>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Set<AiRunStatusEntity>().AsNoTracking().OrderBy(x => x.NameStatus).ToListAsync(cancellationToken);
    public Task<AiRunStatusEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => context.Set<AiRunStatusEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task AddAsync(AiRunStatusEntity aiRunStatus, CancellationToken cancellationToken) => await context.Set<AiRunStatusEntity>().AddAsync(aiRunStatus, cancellationToken);
    public Task UpdateAsync(AiRunStatusEntity aiRunStatus, CancellationToken cancellationToken) { context.Set<AiRunStatusEntity>().Update(aiRunStatus); return Task.CompletedTask; }
    public Task DeleteAsync(AiRunStatusEntity aiRunStatus, CancellationToken cancellationToken) { context.Set<AiRunStatusEntity>().Remove(aiRunStatus); return Task.CompletedTask; }
}
