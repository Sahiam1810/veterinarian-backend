using Domain.Common;
using Domain.Species.ValueObjects;

namespace Domain.Species.Entities;

public sealed class SpeciesEntity : BaseEntity<Guid>
{
    private SpeciesEntity()
    {
    }

    public SpeciesEntity(string name)
    {
        Id = Guid.NewGuid();
        Name = SpeciesName.Create(name);
    }

    public SpeciesName Name { get; private set; } = null!;

    public void Update(string name)
    {
        Name = SpeciesName.Create(name);
        UpdatedAt = DateTime.UtcNow;
    }
}