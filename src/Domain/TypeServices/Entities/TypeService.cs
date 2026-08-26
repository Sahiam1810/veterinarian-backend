using Domain.Common;

namespace Domain.TypeServices.Entities;

public sealed class TypeService : BaseEntity<Guid>
{
    private TypeService()
    {
    }

    public TypeService(string name, string? description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
    }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}
