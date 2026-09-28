using Domain.HospitalizationSettings.Entities;
using HospitalizationSettingsEntity = Domain.HospitalizationSettings.Entities.HospitalizationSettings;

namespace Application.HospitalizationSettings.Abstraction;

public interface IHospitalizationSettingsRepository
{
    Task<HospitalizationSettingsEntity?> GetAsync(CancellationToken cancellationToken);

    Task AddAsync(HospitalizationSettingsEntity settings, CancellationToken cancellationToken);

    Task UpdateAsync(HospitalizationSettingsEntity settings, CancellationToken cancellationToken);
}
