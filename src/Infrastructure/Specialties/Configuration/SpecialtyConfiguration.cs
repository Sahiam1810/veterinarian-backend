using Domain.Specialties.Entities;
using Domain.Specialties.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Specialties.Configuration;

public sealed class SpecialtyConfiguration : IEntityTypeConfiguration<SpecialtyEntity>
{
    public void Configure(EntityTypeBuilder<SpecialtyEntity> builder)
    {
        builder.ToTable("SPECIALTIES");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("SPECIALTY_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(guid => guid
            .ToString(), value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasColumnName("NAME")
            .HasColumnType("VARCHAR2(120)")
            .HasMaxLength(SpecialtyName.MaxLength)
            .HasConversion(name => name.Value, value => SpecialtyName.Create(value))
            .IsRequired();
        builder.Property(x => x.Description)
            .HasColumnName("DESCRIPTION")
            .HasColumnType("VARCHAR2(120)")
            .HasMaxLength(SpecialtyDescription.MaxLength)
            .HasConversion(description => description.Value, value => SpecialtyDescription
            .Create(value))
            .IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnName("CREATED_AT").HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UPDATED_AT").HasColumnType("TIMESTAMP");
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
