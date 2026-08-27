using Domain.MedicalRecords.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MedicalRecords.Configuration;

public sealed class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
{
    public void Configure(EntityTypeBuilder<MedicalRecord> builder)
    {
        builder.ToTable("MEDICAL_RECORDS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("RECORD_ID")
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

        builder.Property(x => x.AppointmentId)
            .HasColumnName("APPOINTMENT_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(x => x.DiagnosticId)
            .HasColumnName("DIAGNOSTIC_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(x => x.Symptoms)
            .HasColumnName("SYMPTOMS")
            .HasColumnType("VARCHAR2(30)")
            .HasMaxLength(30);

        builder.Property(x => x.Treatment)
            .HasColumnName("TREATMENT")
            .HasColumnType("VARCHAR2(30)")
            .HasMaxLength(30);

        builder.Property(x => x.WeightAtVisit)
            .HasColumnName("WEIGHT_AT_VISIT")
            .HasColumnType("NUMBER");

        builder.Property(x => x.Temperature)
            .HasColumnName("TEMPERATURE")
            .HasColumnType("NUMBER");

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

        builder.HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Diagnostic)
            .WithMany()
            .HasForeignKey(x => x.DiagnosticId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
