using Domain.Common;
using Domain.Priorities.ValueObjects;

namespace Domain.Priorities.Entities;

// Entidad de catálogo para prioridades.
public sealed class PriorityEntity : BaseEntity<Guid>
{
    private PriorityEntity() { }

    public PriorityEntity(string name)
    {
        Id = Guid.NewGuid();
        Name = PriorityName.Create(name);
    }

    public PriorityName Name { get; private set; } = null!;

    // Actualiza el nombre de la prioridad.
    public void Update(string name)
    {
        Name = PriorityName.Create(name);
        UpdatedAt = DateTime.UtcNow;
    }
}
