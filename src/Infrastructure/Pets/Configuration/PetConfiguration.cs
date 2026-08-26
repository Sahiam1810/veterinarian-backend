using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Pets.Entities;
using Domain.Pets.ValueObjects;
using veterinarian_backend.Domain.Races.Entities;
using veterinarian_backend.Domain.Species.Entities;

namespace Infrastructure.Pets.Configuration;

public sealed class PetConfiguration : IEntityTypeConfiguration<PetEntity>
{
    public void Configure(EntityTypeBuilder<PetEntity> builder)
    {
        builder.ToTable("PETS");

        builder.HasKey(pet => pet.Id);

        builder.Property(pet => pet.Id)
            .HasColumnName("PET_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        builder.Property(pet => pet.Name)
            .HasColumnName("NAME")
            .HasMaxLength(PetName.MaxLength)
            .HasConversion(
                name => name.Value,
                str => PetName.Create(str))
            .IsRequired();

        builder.Property(pet => pet.Age)
            .HasColumnName("AGE")
            .IsRequired();

        builder.Property(pet => pet.Gender)
            .HasColumnName("GENDER")
            .HasMaxLength(1)
            .HasConversion(
                gender => gender.Value,
                str => PetGender.Create(str))
            .IsRequired();

        builder.Property(pet => pet.Weight)
            .HasColumnName("WEIGHT")
            .HasColumnType("NUMBER(6,3)")
            .HasConversion(
                weight => weight.Value,
                val => PetWeight.Create(val))
            .IsRequired();

        builder.Property(pet => pet.Observations)
            .HasColumnName("OBSERVATIONS")
            .HasMaxLength(PetObservations.MaxLength)
            .HasConversion(
                obs => obs.Value,
                str => PetObservations.Create(str))
            .IsRequired(false);

        builder.Property(pet => pet.SpeciesId)
            .HasColumnName("SPECIES_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        builder.Property(pet => pet.RaceId)
            .HasColumnName("RACE_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        builder.Property(pet => pet.CreatedAt)
            .HasColumnName("CREATED_AT")
            .IsRequired();

        builder.Property(pet => pet.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .IsRequired(false);

        builder.HasOne(pet => pet.Species)
            .WithMany()
            .HasForeignKey(pet => pet.SpeciesId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pet => pet.Race)
            .WithMany()
            .HasForeignKey(pet => pet.RaceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
