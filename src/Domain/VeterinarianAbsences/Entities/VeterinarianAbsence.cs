using Domain.Common;
using Domain.Veterinarians.Entities;

namespace Domain.VeterinarianAbsences.Entities;

// Ausencia puntual; no sustituye el horario semanal recurrente.
public sealed class VeterinarianAbsence : BaseEntity<Guid>
{
    public const int ReasonMaxLength = 200;

    private VeterinarianAbsence()
    {
    }

    public VeterinarianAbsence(
        Guid veterinarianId,
        DateTime startAtUtc,
        DateTime endAtUtc,
        string? reason,
        bool isFullDay)
    {
        Id = Guid.NewGuid();
        VeterinarianId = veterinarianId;
        ApplyWindow(startAtUtc, endAtUtc, isFullDay);
        Reason = NormalizeReason(reason);
    }

    public Guid VeterinarianId { get; private set; }
    public Veterinarian? Veterinarian { get; private set; }
    public DateTime StartAtUtc { get; private set; }
    public DateTime EndAtUtc { get; private set; }
    public string? Reason { get; private set; }
    public bool IsFullDay { get; private set; }

    public void Update(
        DateTime startAtUtc,
        DateTime endAtUtc,
        string? reason,
        bool isFullDay)
    {
        ApplyWindow(startAtUtc, endAtUtc, isFullDay);
        Reason = NormalizeReason(reason);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool Overlaps(DateTime startUtc, DateTime endUtc) =>
        StartAtUtc < endUtc && EndAtUtc > startUtc;

    private void ApplyWindow(DateTime startAtUtc, DateTime endAtUtc, bool isFullDay)
    {
        if (endAtUtc <= startAtUtc)
        {
            throw new ArgumentException(
                "La fecha de fin de la ausencia debe ser posterior al inicio.");
        }

        StartAtUtc = DateTime.SpecifyKind(startAtUtc, DateTimeKind.Utc);
        EndAtUtc = DateTime.SpecifyKind(endAtUtc, DateTimeKind.Utc);
        IsFullDay = isFullDay;
    }

    private static string? NormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var value = reason.Trim();
        if (value.Length > ReasonMaxLength)
        {
            throw new ArgumentException(
                $"El motivo no puede superar los {ReasonMaxLength} caracteres.",
                nameof(reason));
        }

        return value;
    }
}
