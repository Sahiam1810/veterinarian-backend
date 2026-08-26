using Domain.Species.Entities;
using Domain.Species.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
            .HasMaxLength(SpeciesName.MaxLength)
            .HasConversion(
                name => name.Value,
                str => SpeciesName.Create(str))
            .IsRequired();

        builder.Property(species => species.CreatedAt)
            .HasColumnName("CREATED_AT")
            .IsRequired();

        builder.Property(species => species.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .IsRequired(false);
    }
}
