using Domain.Common;

namespace Domain.Medications.Entities;

public sealed class Medication : BaseEntity<Guid>
{
    private Medication()
    {
    }

    public Medication(string name, string? code = null, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del medicamento es obligatorio.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Code = NormalizeCode(code);
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }

    public string Name { get; private set; } = null!;
    public string? Code { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, string? code, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del medicamento es obligatorio.", nameof(name));
        }

        Name = name.Trim();
        Code = NormalizeCode(code);
        IsActive = isActive;
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

    public static string? NormalizeCode(string? code)
    {
        return string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
    }
}
