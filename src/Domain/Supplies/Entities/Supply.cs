using Domain.Common;

namespace Domain.Supplies.Entities;

public sealed class Supply : BaseEntity<Guid>
{
    private Supply()
    {
    }

    public Supply(string name, string unit, decimal unitPrice, decimal stock, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del insumo es obligatorio.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("La unidad del insumo es obligatoria.", nameof(unit));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "El precio unitario no puede ser negativo.");
        }

        if (stock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stock), "El stock inicial no puede ser negativo.");
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Unit = unit.Trim();
        UnitPrice = unitPrice;
        Stock = stock;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }

    public string Name { get; private set; } = null!;
    public string Unit { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public decimal Stock { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, string unit, decimal unitPrice, decimal stock, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del insumo es obligatorio.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("La unidad del insumo es obligatoria.", nameof(unit));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "El precio unitario no puede ser negativo.");
        }

        if (stock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stock), "El stock no puede ser negativo.");
        }

        Name = name.Trim();
        Unit = unit.Trim();
        UnitPrice = unitPrice;
        Stock = stock;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DeductStock(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad consumida debe ser mayor a 0.");
        }

        if (quantity > Stock)
        {
            throw new InvalidOperationException("Stock insuficiente.");
        }

        Stock -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
