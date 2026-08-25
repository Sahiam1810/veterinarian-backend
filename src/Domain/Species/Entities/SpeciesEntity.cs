namespace veterinarian_backend.Domain.Species.Entities;

public class SpeciesEntity
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }
}