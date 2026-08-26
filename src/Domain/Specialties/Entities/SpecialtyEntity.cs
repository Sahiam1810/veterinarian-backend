using Domain.Common;
using Domain.Specialties.ValueObjects;

namespace Domain.Specialties.Entities;

public sealed class SpecialtyEntity : BaseEntity<Guid>
{
    private SpecialtyEntity() { }

    public SpecialtyEntity(string name, string? description)
    {
        Id = Guid.NewGuid();
        Name = SpecialtyName.Create(name);
        Description = SpecialtyDescription.Create(description);
    }

    public SpecialtyName Name { get; private set; } = null!;
    public SpecialtyDescription Description { get; private set; } = null!;

    public void Update(string name, string? description)
    {
        Name = SpecialtyName.Create(name);
        Description = SpecialtyDescription.Create(description);
        UpdatedAt = DateTime.UtcNow;
    }
}
