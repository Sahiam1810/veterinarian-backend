using Domain.VeterinarianAbsences.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.VeterinarianAbsences.Configuration;

public sealed class VeterinarianAbsenceConfiguration : IEntityTypeConfiguration<VeterinarianAbsence>
{
    public void Configure(EntityTypeBuilder<VeterinarianAbsence> builder)
    {
        builder.ToTable("VETERINARIAN_ABSENCES");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("ABSENCE_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.VeterinarianId)
            .HasColumnName("VETERINARIAN_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(x => x.StartAtUtc)
            .HasColumnName("START_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.EndAtUtc)
            .HasColumnName("END_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasColumnName("REASON")
            .HasColumnType("VARCHAR2(200)")
            .HasMaxLength(VeterinarianAbsence.ReasonMaxLength);

        builder.Property(x => x.IsFullDay)
            .HasColumnName("IS_FULL_DAY")
            .HasColumnType("CHAR(1)")
            .HasConversion(
                b => b ? 'Y' : 'N',
                c => c == 'Y')
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP");

        builder.HasOne(x => x.Veterinarian)
            .WithMany()
            .HasForeignKey(x => x.VeterinarianId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.VeterinarianId);
        builder.HasIndex(x => new { x.VeterinarianId, x.StartAtUtc, x.EndAtUtc });
    }
}
