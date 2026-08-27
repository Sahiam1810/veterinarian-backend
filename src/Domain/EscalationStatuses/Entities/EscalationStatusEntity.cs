using Domain.Common;
using Domain.EscalationStatuses.ValueObjects;

namespace Domain.EscalationStatuses.Entities;

// Entidad de catálogo para estados de escalamiento.
public sealed class EscalationStatusEntity : BaseEntity<Guid>
{
    private EscalationStatusEntity() { }

    public EscalationStatusEntity(string name)
    {
        Id = Guid.NewGuid();
        Name = EscalationStatusName.Create(name);
    }

    public EscalationStatusName Name { get; private set; } = null!;

    // Actualiza el nombre del estado.
    public void Update(string name)
    {
        Name = EscalationStatusName.Create(name);
        UpdatedAt = DateTime.UtcNow;
    }
}
