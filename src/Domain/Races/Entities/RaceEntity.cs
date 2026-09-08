using Domain.Common;
using Domain.Races.ValueObjects;
using Domain.Species.Entities;

namespace Domain.Races.Entities;

// Raza siempre pertenece a una especie (no hay Golden de Conejo).
public sealed class RaceEntity : BaseEntity<Guid>
{
    private RaceEntity()
    {
    }

    public RaceEntity(string name, Guid speciesId)
    {
        Id = Guid.NewGuid();
        Name = RaceName.Create(name);
        SpeciesId = speciesId;
    }

    public RaceEntity(string name, SpeciesEntity species)
        : this(name, species.Id)
    {
        Species = species;
    }

    public RaceName Name { get; private set; } = null!;
    public Guid SpeciesId { get; private set; }
    public SpeciesEntity Species { get; private set; } = null!;

    public void Update(string name, Guid speciesId)
    {
        Name = RaceName.Create(name);
        SpeciesId = speciesId;
        UpdatedAt = DateTime.UtcNow;
    }
}
