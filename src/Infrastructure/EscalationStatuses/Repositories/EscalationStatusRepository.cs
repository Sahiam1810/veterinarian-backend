using Application.EscalationStatuses.Abstraction;
using Domain.EscalationStatuses.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EscalationStatuses.Repositories;

// Repositorio EF Core de estados de escalamiento.
public sealed class EscalationStatusRepository(VeterinaryDbContext context) : IEscalationStatusRepository
{
    public async Task<IReadOnlyCollection<EscalationStatusEntity>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Set<EscalationStatusEntity>()
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<EscalationStatusEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Set<EscalationStatusEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(EscalationStatusEntity escalationStatus, CancellationToken cancellationToken) =>
        await context.Set<EscalationStatusEntity>().AddAsync(escalationStatus, cancellationToken);

    public Task UpdateAsync(EscalationStatusEntity escalationStatus, CancellationToken cancellationToken)
    {
        context.Set<EscalationStatusEntity>().Update(escalationStatus);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(EscalationStatusEntity escalationStatus, CancellationToken cancellationToken)
    {
        context.Set<EscalationStatusEntity>().Remove(escalationStatus);
        return Task.CompletedTask;
    }
}
