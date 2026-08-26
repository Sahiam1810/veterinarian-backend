using Domain.Common;
using Domain.TypeServices.Entities;

namespace Domain.Services.Entities;

public sealed class Service : BaseEntity<Guid>
{
    private Service()
    {
    }

    public Service(
        Guid typeServiceId,
        string name,
        int durationMinutes,
        decimal price,
        bool isActive = true)
    {
        Id = Guid.NewGuid();
        TypeServiceId = typeServiceId;
        Name = name;
        DurationMinutes = durationMinutes;
        Price = price;
        IsActive = isActive;
    }

    public Guid TypeServiceId { get; private set; }
    public TypeService? TypeService { get; private set; }

    public string Name { get; private set; } = null!;
    public int DurationMinutes { get; private set; }
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        Guid typeServiceId,
        string name,
        int durationMinutes,
        decimal price,
        bool isActive)
    {
        TypeServiceId = typeServiceId;
        Name = name;
        DurationMinutes = durationMinutes;
        Price = price;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
