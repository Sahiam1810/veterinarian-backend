using Domain.Common;

namespace Domain.Procedures.Entities;

public sealed class Procedure : BaseEntity<Guid>
{
    private Procedure()
    {
    }

    public Procedure(string name, string? code = null, bool isActive = true, decimal price = 0m)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del procedimiento es obligatorio.", nameof(name));
        }

        if (price < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "El precio no puede ser negativo.");
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
            throw new ArgumentException("El nombre del procedimiento es obligatorio.", nameof(name));
        }

        Name = name.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        IsActive = isActive;
        if (price.HasValue)
        {
            if (price.Value < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(price), "El precio no puede ser negativo.");
            }

            Price = price.Value;
        }
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
