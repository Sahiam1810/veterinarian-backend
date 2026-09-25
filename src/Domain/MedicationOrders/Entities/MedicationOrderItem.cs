using Domain.Common;
using Domain.Medications.Entities;

namespace Domain.MedicationOrders.Entities;

public sealed class MedicationOrderItem : BaseEntity<Guid>
{
    private MedicationOrderItem()
    {
    }

    public MedicationOrderItem(
        Guid medicationOrderId,
        Guid medicationId,
        string? notes)
    {
        if (medicationOrderId == Guid.Empty)
        {
            throw new ArgumentException("El ID de la orden de medicamento es obligatorio.", nameof(medicationOrderId));
        }

        if (medicationId == Guid.Empty)
        {
            throw new ArgumentException("El ID del medicamento es obligatorio.", nameof(medicationId));
        }

        Id = Guid.NewGuid();
        MedicationOrderId = medicationOrderId;
        MedicationId = medicationId;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public Guid MedicationOrderId { get; private set; }
    public Guid MedicationId { get; private set; }
    public Medication? Medication { get; private set; }
    public decimal? UnitPrice { get; private set; }
    public string? Notes { get; private set; }

    public void SetUnitPrice(decimal? unitPrice)
    {
        UnitPrice = unitPrice;
        UpdatedAt = DateTime.UtcNow;
    }
}
