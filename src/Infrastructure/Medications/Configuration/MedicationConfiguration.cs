using Domain.Medications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Medications.Configuration;

public class MedicationConfiguration : IEntityTypeConfiguration<Medication>
{
    public void Configure(EntityTypeBuilder<Medication> builder)
    {
        builder.ToTable("MEDICATIONS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("MEDICATION_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("NAME")
            .HasColumnType("VARCHAR2(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasColumnName("CODE")
            .HasColumnType("VARCHAR2(50)")
            .HasMaxLength(50)
            .IsRequired(false);

        // La migraciÃ³n del Ã­ndice Ãºnico la genera y ejecuta la responsable del proyecto.
        builder.HasIndex(x => x.Code)
            .HasFilter(null)
            .IsUnique();

        builder.Property(x => x.IsActive)
            .HasColumnName("IS_ACTIVE")
            .HasConversion<int>()
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.Price)
            .HasColumnName("PRICE")
            .HasColumnType("DECIMAL(18,2)")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .IsRequired(false);
    }
}
