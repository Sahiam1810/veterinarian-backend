using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using veterinarian_backend.Domain.Races.Entities;

namespace Infrastructure.Races.Configuration;

public sealed class RacesConfiguration : IEntityTypeConfiguration<RaceEntity>
{
    public void Configure(EntityTypeBuilder<RaceEntity> builder)
    {
        builder.ToTable("RACES");

        builder.HasKey(race => race.Id);

        builder.Property(race => race.Id)
            .HasColumnName("RACE_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        builder.Property(race => race.Name)
            .HasColumnName("NAME")
            .HasMaxLength(20) 
            .IsRequired();
    }
}
