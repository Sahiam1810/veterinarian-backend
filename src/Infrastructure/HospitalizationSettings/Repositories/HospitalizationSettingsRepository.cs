using Application.HospitalizationSettings.Abstraction;
using Domain.HospitalizationSettings.Entities;
using HospitalizationSettingsEntity = Domain.HospitalizationSettings.Entities.HospitalizationSettings;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.HospitalizationSettings.Repositories;

public sealed class HospitalizationSettingsRepository(VeterinaryDbContext context)
    : IHospitalizationSettingsRepository
{
    public Task<HospitalizationSettingsEntity?> GetAsync(CancellationToken cancellationToken) =>
        context.Set<HospitalizationSettingsEntity>()
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(
        HospitalizationSettingsEntity settings,
        CancellationToken cancellationToken) =>
        await context.Set<HospitalizationSettingsEntity>().AddAsync(settings, cancellationToken);

    public Task UpdateAsync(
        HospitalizationSettingsEntity settings,
        CancellationToken cancellationToken)
    {
        context.Set<HospitalizationSettingsEntity>().Update(settings);
        return Task.CompletedTask;
    }
}
