using Domain.Common;
using Domain.Races.ValueObjects;
using Domain.Species.Entities;

namespace Domain.Races.Entities;

public sealed class RaceEntity : BaseEntity<Guid>
{
    private RaceEntity()
    {
    }

    public RaceEntity(string name, SpeciesEntity species)
    {
        Id = Guid.NewGuid();
        Name = RaceName.Create(name);
        Species = species ?? throw new ArgumentNullException(nameof(species));
        SpeciesId = species.Id;
    }

    public RaceName Name { get; private set; } = null!;
    public Guid SpeciesId { get; private set; }
    public SpeciesEntity Species { get; private set; } = null!;

    public void Update(string name, SpeciesEntity species)
    {
        Name = RaceName.Create(name);
        Species = species ?? throw new ArgumentNullException(nameof(species));
        SpeciesId = species.Id;
        UpdatedAt = DateTime.UtcNow;
    }
}
