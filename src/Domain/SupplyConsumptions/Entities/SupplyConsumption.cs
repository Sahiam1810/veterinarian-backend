using Domain.Common;

namespace Domain.SupplyConsumptions.Entities;

public sealed class SupplyConsumption : BaseEntity<Guid>
{
    private SupplyConsumption()
    {
    }

    public SupplyConsumption(
        Guid hospitalizationStayId,
        Guid supplyId,
        decimal quantity,
        decimal unitPrice,
        Guid registeredByUserId,
        string? notes = null)
    {
        if (hospitalizationStayId == Guid.Empty)
        {
            throw new ArgumentException("El ID de la estancia de hospitalización es obligatorio.", nameof(hospitalizationStayId));
        }

        if (supplyId == Guid.Empty)
        {
            throw new ArgumentException("El ID del insumo es obligatorio.", nameof(supplyId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad consumida debe ser mayor a 0.");
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "El precio unitario no puede ser negativo.");
        }

        if (registeredByUserId == Guid.Empty)
        {
            throw new ArgumentException("El ID del usuario que registra es obligatorio.", nameof(registeredByUserId));
        }

        Id = Guid.NewGuid();
        HospitalizationStayId = hospitalizationStayId;
        SupplyId = supplyId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Total = quantity * unitPrice;
        RegisteredByUserId = registeredByUserId;
        Notes = notes?.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public Guid HospitalizationStayId { get; private set; }
    public Guid SupplyId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Total { get; private set; }
    public Guid RegisteredByUserId { get; private set; }
    public string? Notes { get; private set; }
}
