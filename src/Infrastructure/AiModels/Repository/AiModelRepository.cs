using Application.AiModels.Abstraction;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using AiModelEntity = Domain.AiModels.Entities.AiModel;

namespace Infrastructure.AiModels.Repository;

public sealed class AiModelRepository : IAiModelRepository
{
    private readonly VeterinaryDbContext _context;

    public AiModelRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<AiModelEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await _context.Set<AiModelEntity>()
            .AsNoTracking()
            .OrderBy(model => model.NameModel)
            .ToListAsync(cancellationToken);

    public Task<AiModelEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<AiModelEntity>()
            .FirstOrDefaultAsync(model => model.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<AiModelEntity>> GetByProviderIdAsync(
        Guid providerModelAiId,
        CancellationToken cancellationToken = default)
        => await _context.Set<AiModelEntity>()
            .AsNoTracking()
            .Where(model => model.ProviderModelAiId == providerModelAiId)
            .OrderBy(model => model.NameModel)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        AiModelEntity model,
        CancellationToken cancellationToken = default)
        => await _context.Set<AiModelEntity>().AddAsync(model, cancellationToken);

    public Task UpdateAsync(
        AiModelEntity model,
        CancellationToken cancellationToken = default)
    {
        _context.Set<AiModelEntity>().Update(model);
        return Task.CompletedTask;
    }
}
