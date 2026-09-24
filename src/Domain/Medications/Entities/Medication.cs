using Domain.Common;

namespace Domain.Medications.Entities;

public sealed class Medication : BaseEntity<Guid>
{
    private Medication()
    {
    }

    public Medication(string name, string? code = null, bool isActive = true, decimal price = 0m)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del medicamento es obligatorio.", nameof(name));
        }

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "El precio del medicamento no puede ser negativo.");
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        IsActive = isActive;
        Price = price;
        CreatedAt = DateTime.UtcNow;
    }

    public string Name { get; private set; } = null!;
    public string? Code { get; private set; }
    public bool IsActive { get; private set; }
    public decimal Price { get; private set; }

    public void Update(string name, string? code, bool isActive, decimal? price = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del medicamento es obligatorio.", nameof(name));
        }

        var nextPrice = price ?? Price;
        if (nextPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "El precio del medicamento no puede ser negativo.");
        }

        Name = name.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        IsActive = isActive;
        Price = nextPrice;
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
