using Domain.Vaccinations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Vaccinations.Configuration;

public sealed class VaccinationConfiguration : IEntityTypeConfiguration<Vaccination>
{
    public void Configure(EntityTypeBuilder<Vaccination> builder)
    {
        builder.ToTable("VACCINATIONS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("VACCINATION_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.ClientPetId)
            .HasColumnName("CLIENT_PET_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(x => x.RecordId)
            .HasColumnName("RECORD_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(x => x.VaccineName)
            .HasColumnName("VACCINE_NAME")
            .HasColumnType("VARCHAR2(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.DoseNumber)
            .HasColumnName("DOSE_NUMBER")
            .HasColumnType("NUMBER")
            .IsRequired();

        builder.Property(x => x.ApplicationDate)
            .HasColumnName("APPLICATION_DATE")
            .HasColumnType("DATE")
            .IsRequired();

        builder.Property(x => x.NextDoseDate)
            .HasColumnName("NEXT_DOSE_DATE")
            .HasColumnType("DATE");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATE_AT")
            .HasColumnType("TIMESTAMP");

        builder.HasOne(x => x.ClientPet)
            .WithMany()
            .HasForeignKey(x => x.ClientPetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Record)
            .WithMany()
            .HasForeignKey(x => x.RecordId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
