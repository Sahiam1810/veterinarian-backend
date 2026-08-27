using Domain.Common;
using Domain.SenderTypes.ValueObjects;

namespace Domain.SenderTypes.Entities;

public sealed class SenderTypeEntity : BaseEntity<Guid>
{
    private SenderTypeEntity() { }

    public SenderTypeEntity(string name)
    {
        Id = Guid.NewGuid();
        Name = SenderTypeName.Create(name);
    }

    public SenderTypeName Name { get; private set; } = null!;

    public void Update(string name)
    {
        Name = SenderTypeName.Create(name);
        UpdatedAt = DateTime.UtcNow;
    }
}
