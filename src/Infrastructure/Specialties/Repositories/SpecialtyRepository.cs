using Application.Specialties.Abstraction;
using Domain.Specialties.Entities;
using Domain.Specialties.ValueObjects;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Specialties.Repositories;

public sealed class SpecialtyRepository(VeterinaryDbContext context) : ISpecialtyRepository
{
    public async Task<IReadOnlyCollection<SpecialtyEntity>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Set<SpecialtyEntity>().AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
    public Task<SpecialtyEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Set<SpecialtyEntity>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken, Guid? excludedId = null)
    {
        var nameVo = SpecialtyName.Create(name);

        return context.Set<SpecialtyEntity>().AnyAsync(
            x => x.Name == nameVo && (!excludedId.HasValue || x.Id != excludedId.Value),
            cancellationToken);
    }
    public async Task AddAsync(SpecialtyEntity specialty, CancellationToken cancellationToken) => await context.Set<SpecialtyEntity>().AddAsync(specialty, cancellationToken);
    public Task UpdateAsync(SpecialtyEntity specialty, CancellationToken cancellationToken) { context.Set<SpecialtyEntity>().Update(specialty); return Task.CompletedTask; }
    public Task DeleteAsync(SpecialtyEntity specialty, CancellationToken cancellationToken) { context.Set<SpecialtyEntity>().Remove(specialty); return Task.CompletedTask; }
}
