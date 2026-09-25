using Domain.HospitalizationStays.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.HospitalizationStays.Configuration;

public sealed class HospitalizationNoteConfiguration : IEntityTypeConfiguration<HospitalizationNote>
{
    public void Configure(EntityTypeBuilder<HospitalizationNote> builder)
    {
        builder.ToTable("HOSPITALIZATION_NOTES");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.StayId)
            .HasColumnName("STAY_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(x => x.AutorUserId)
            .HasColumnName("AUTOR_USER_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(x => x.FechaHora)
            .HasColumnName("FECHA_HORA")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.Nota)
            .HasColumnName("NOTA")
            .HasColumnType("VARCHAR2(2000)")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.EntregadoAUserId)
            .HasColumnName("ENTREGADO_A_USER_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.HasValue ? guid.Value.ToString() : null,
                value => string.IsNullOrWhiteSpace(value) ? null : Guid.Parse(value))
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATE_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);

        builder.HasIndex(x => x.StayId)
            .HasDatabaseName("IX_HOSP_NOTE_STAY_ID");

        builder.HasOne<HospitalizationStay>()
            .WithMany()
            .HasForeignKey(x => x.StayId)
            .HasConstraintName("FK_HOSPITALIZATION_NOTES_STAY")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
