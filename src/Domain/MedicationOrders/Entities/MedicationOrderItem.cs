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
        string? notes,
        decimal unitPrice = 0m)
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
        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "El precio unitario no puede ser negativo.");
        }

        UnitPrice = unitPrice;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid MedicationOrderId { get; private set; }
    public Guid MedicationId { get; private set; }
    public Medication? Medication { get; private set; }
    public string? Notes { get; private set; }
    public decimal UnitPrice { get; private set; }
}
