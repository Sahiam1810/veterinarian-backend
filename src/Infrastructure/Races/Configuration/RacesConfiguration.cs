using Domain.Races.Entities;
using Domain.Races.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
            .HasMaxLength(RaceName.MaxLength)
            .HasConversion(
                name => name.Value,
                str => RaceName.Create(str))
            .IsRequired();

        builder.Property(race => race.CreatedAt)
            .HasColumnName("CREATED_AT")
            .IsRequired();

        builder.Property(race => race.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .IsRequired(false);
    }
}
