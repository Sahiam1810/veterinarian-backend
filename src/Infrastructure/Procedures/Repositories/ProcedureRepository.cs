using Application.Procedures.Abstraction;
using Domain.Procedures.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Procedures.Repositories;

public sealed class ProcedureRepository(VeterinaryDbContext context) : IProcedureRepository
{
    public async Task<IEnumerable<Procedure>> GetAllAsync(bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var query = context.Procedures.AsQueryable();
        if (onlyActive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public async Task<Procedure?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Procedures.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task AddAsync(Procedure procedure, CancellationToken cancellationToken = default)
    {
        await context.Procedures.AddAsync(procedure, cancellationToken);
    }

    public Task UpdateAsync(Procedure procedure, CancellationToken cancellationToken = default)
    {
        context.Procedures.Update(procedure);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var procedure = await GetByIdAsync(id, cancellationToken);
        if (procedure is not null)
        {
            procedure.Deactivate();
            context.Procedures.Update(procedure);
        }
    }
}
