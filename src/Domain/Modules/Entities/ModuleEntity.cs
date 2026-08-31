using Domain.Common;
using Domain.Modules.ValueObjects;

namespace Domain.Modules.Entities;

public sealed class ModuleEntity : BaseEntity<Guid>
{
    private ModuleEntity()
    {
    }

    public ModuleEntity(string name, string? description)
    {
        Id = Guid.NewGuid();
        Name = ModuleName.Create(name);
        Description = description;
    }

    public ModuleName Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public void Update(string name, string? description)
    {
        Name = ModuleName.Create(name);
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}
