using Application.TypeServices.Abstraction;
using Domain.TypeServices.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.TypeServices.Repositories;

public sealed class TypeServiceRepository : ITypeServiceRepository
{
    private readonly VeterinaryDbContext _context;

    public TypeServiceRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<TypeService>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<TypeService>()
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<TypeService?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<TypeService>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default,
        Guid? excludedId = null)
        => _context.Set<TypeService>()
            .AnyAsync(
                x => x.Name == name
                    && (!excludedId.HasValue || x.Id != excludedId.Value),
                cancellationToken);

    public async Task AddAsync(
        TypeService typeService,
        CancellationToken cancellationToken = default)
        => await _context.Set<TypeService>()
            .AddAsync(typeService, cancellationToken);

    public Task UpdateAsync(
        TypeService typeService,
        CancellationToken cancellationToken = default)
    {
        _context.Set<TypeService>().Update(typeService);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        TypeService typeService,
        CancellationToken cancellationToken = default)
    {
        _context.Set<TypeService>().Remove(typeService);
        return Task.CompletedTask;
    }
}
