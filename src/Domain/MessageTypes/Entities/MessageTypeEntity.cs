using Domain.Common;
using Domain.MessageTypes.ValueObjects;

namespace Domain.MessageTypes.Entities;

public sealed class MessageTypeEntity : BaseEntity<Guid>
{
    private MessageTypeEntity() { }

    public MessageTypeEntity(string name)
    {
        Id = Guid.NewGuid();
        Name = MessageTypeName.Create(name);
    }

    public MessageTypeName Name { get; private set; } = null!;

    public void Update(string name)
    {
        Name = MessageTypeName.Create(name);
        UpdatedAt = DateTime.UtcNow;
    }
}
