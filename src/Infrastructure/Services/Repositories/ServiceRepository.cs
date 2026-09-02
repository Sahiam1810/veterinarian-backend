using Application.Services.Abstraction;
using Domain.Services.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Repositories;

public sealed class ServiceRepository : IServiceRepository
{
    private readonly VeterinaryDbContext _context;

    public ServiceRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<Service>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<Service>()
            .Include(x => x.TypeService)
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Service>> GetAvailableAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<Service>()
            .Include(x => x.TypeService)
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<Service?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<Service>()
            .Include(x => x.TypeService)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default,
        Guid? excludedId = null)
        => _context.Set<Service>()
            .AnyAsync(
                x => x.Name == name
                    && (!excludedId.HasValue || x.Id != excludedId.Value),
                cancellationToken);

    public async Task AddAsync(
        Service service,
        CancellationToken cancellationToken = default)
        => await _context.Set<Service>()
            .AddAsync(service, cancellationToken);

    public Task UpdateAsync(
        Service service,
        CancellationToken cancellationToken = default)
    {
        _context.Set<Service>().Update(service);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        Service service,
        CancellationToken cancellationToken = default)
    {
        _context.Set<Service>().Remove(service);
        return Task.CompletedTask;
    }
}
