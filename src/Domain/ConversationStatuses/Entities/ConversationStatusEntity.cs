using Domain.Common;
using Domain.ConversationStatuses.ValueObjects;

namespace Domain.ConversationStatuses.Entities;

// Entidad de catálogo para estados de conversación.
public sealed class ConversationStatusEntity : BaseEntity<Guid>
{
    private ConversationStatusEntity() { }

    public ConversationStatusEntity(string name)
    {
        Id = Guid.NewGuid();
        Name = ConversationStatusName.Create(name);
    }

    public ConversationStatusName Name { get; private set; } = null!;

    // Actualiza el nombre del estado.
    public void Update(string name)
    {
        Name = ConversationStatusName.Create(name);
        UpdatedAt = DateTime.UtcNow;
    }
}
