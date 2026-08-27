using Application.ProviderModelsAi.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ProviderEntity = Domain.ProviderModelsAi.Entities.ProviderModelAi;

namespace Infrastructure.ProviderModelsAi.Repository;

public sealed class ProviderModelAiRepository : IProviderModelAiRepository
{
    private readonly VeterinaryDbContext _context;

    public ProviderModelAiRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ProviderEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<ProviderEntity>()
            .AsNoTracking()
            .OrderBy(provider => provider.NameProviderAi)
            .ToListAsync(cancellationToken);

    public Task<ProviderEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<ProviderEntity>()
            .FirstOrDefaultAsync(provider => provider.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<ProviderEntity>()
            .AnyAsync(provider => provider.Id == id, cancellationToken);

    public async Task AddAsync(
        ProviderEntity provider,
        CancellationToken cancellationToken = default)
        => await _context.Set<ProviderEntity>().AddAsync(provider, cancellationToken);

    public Task UpdateAsync(
        ProviderEntity provider,
        CancellationToken cancellationToken = default)
    {
        _context.Set<ProviderEntity>().Update(provider);
        return Task.CompletedTask;
    }
}
