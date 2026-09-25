using Domain.Common;
using Domain.Procedures.Entities;

namespace Domain.ProcedureOrders.Entities;

public sealed class ProcedureOrderItem : BaseEntity<Guid>
{
    private ProcedureOrderItem()
    {
    }

    public ProcedureOrderItem(
        Guid procedureOrderId,
        Guid procedureId,
        string? notes)
    {
        if (procedureOrderId == Guid.Empty)
        {
            throw new ArgumentException("El ID de la orden de procedimiento es obligatorio.", nameof(procedureOrderId));
        }

        if (procedureId == Guid.Empty)
        {
            throw new ArgumentException("El ID del procedimiento es obligatorio.", nameof(procedureId));
        }

        Id = Guid.NewGuid();
        ProcedureOrderId = procedureOrderId;
        ProcedureId = procedureId;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public Guid ProcedureOrderId { get; private set; }
    public Guid ProcedureId { get; private set; }
    public Procedure? Procedure { get; private set; }
    public decimal? UnitPrice { get; private set; }
    public string? Notes { get; private set; }

    public void SetUnitPrice(decimal? unitPrice)
    {
        UnitPrice = unitPrice;
        UpdatedAt = DateTime.UtcNow;
    }
}
