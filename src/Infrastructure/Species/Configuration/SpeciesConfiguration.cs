using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using veterinarian_backend.Domain.Species.Entities;

namespace Infrastructure.Species.Configuration;

public sealed class SpeciesConfiguration : IEntityTypeConfiguration<SpeciesEntity>
{
    public void Configure(EntityTypeBuilder<SpeciesEntity> builder)
    {
        builder.ToTable("SPECIES");

        builder.HasKey(species => species.Id);

        builder.Property(species => species.Id)
            .HasColumnName("SPECIES_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        builder.Property(species => species.Name)
            .HasColumnName("NAME")
            .HasMaxLength(20)
            .IsRequired();
    }
}
