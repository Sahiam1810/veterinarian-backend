using Domain.Common;
using Domain.Pets.ValueObjects;
using Domain.Species.Entities;
using Domain.Races.Entities;

namespace Domain.Pets.Entities;

public sealed class PetEntity : BaseEntity<Guid>
{
    private PetEntity()
    {
    }

    public PetEntity(
        string name,
        int age,
        string gender,
        decimal weight,
        string? observations,
        SpeciesEntity speciesEntity,
        RaceEntity raceEntity,
        string? photoUrl = null)
    {
        EnsureRaceBelongsToSpecies(speciesEntity, raceEntity);

        Id = Guid.NewGuid();
        Name = PetName.Create(name);
        Age = age;
        Gender = PetGender.Create(gender);
        Weight = PetWeight.Create(weight);
        Observations = PetObservations.Create(observations);
        PhotoUrl = PetPhotoUrl.Create(photoUrl);
        Species = speciesEntity;
        Race = raceEntity;
        SpeciesId = speciesEntity.Id;
        RaceId = raceEntity.Id;
    }

    public PetName Name { get; private set; } = null!;
    public int Age { get; private set; }
    public PetGender Gender { get; private set; } = null!;
    public PetWeight Weight { get; private set; } = null!;
    public PetObservations Observations { get; private set; } = null!;
    // Foto externa por URL; nullable en Oracle
    public PetPhotoUrl PhotoUrl { get; private set; } = null!;
    public Guid SpeciesId { get; private set; }
    public Guid RaceId { get; private set; }

    // Navigation properties
    public SpeciesEntity Species { get; private set; } = null!;
    public RaceEntity Race { get; private set; } = null!;

    public void Update(
        string name,
        int age,
        string gender,
        decimal weight,
        string? observations,
        SpeciesEntity speciesEntity,
        RaceEntity raceEntity,
        string? photoUrl = null)
    {
        EnsureRaceBelongsToSpecies(speciesEntity, raceEntity);

        Name = PetName.Create(name);
        Age = age;
        Gender = PetGender.Create(gender);
        Weight = PetWeight.Create(weight);
        Observations = PetObservations.Create(observations);
        PhotoUrl = PetPhotoUrl.Create(photoUrl);
        Species = speciesEntity;
        Race = raceEntity;
        SpeciesId = speciesEntity.Id;
        RaceId = raceEntity.Id;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void EnsureRaceBelongsToSpecies(
        SpeciesEntity species,
        RaceEntity race)
    {
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(race);

        if (race.SpeciesId != species.Id)
        {
            throw new ArgumentException("La raza no pertenece a la especie seleccionada.", nameof(race));
        }
    }
}
