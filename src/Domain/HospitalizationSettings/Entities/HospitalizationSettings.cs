using Domain.Common;

namespace Domain.HospitalizationSettings.Entities;

/// <summary>
/// Configuración global de facturación del módulo de hospitalización.
/// Es una configuración de clínica, no un servicio de agenda.
/// </summary>
public sealed class HospitalizationSettings : BaseEntity<Guid>
{
    private HospitalizationSettings()
    {
    }

    public HospitalizationSettings(decimal dailyRate)
    {
        Id = Guid.NewGuid();
        SetDailyRate(dailyRate);
    }

    public decimal DailyRate { get; private set; }

    public void SetDailyRate(decimal dailyRate)
    {
        if (dailyRate <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dailyRate),
                "La tarifa diaria de hospitalización debe ser mayor que cero.");
        }

        DailyRate = dailyRate;
        UpdatedAt = DateTime.UtcNow;
    }
}
