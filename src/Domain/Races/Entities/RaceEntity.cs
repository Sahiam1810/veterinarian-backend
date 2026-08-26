using Domain.Common;
using Domain.Races.ValueObjects;

namespace Domain.Races.Entities;

public sealed class RaceEntity : BaseEntity<Guid>
{
    private RaceEntity()
    {
    }

    public RaceEntity(string name)
    {
        Id = Guid.NewGuid();
        Name = RaceName.Create(name);
    }

    public RaceName Name { get; private set; } = null!;

    public void Update(string name)
    {
        Name = RaceName.Create(name);
        UpdatedAt = DateTime.UtcNow;
    }
}
