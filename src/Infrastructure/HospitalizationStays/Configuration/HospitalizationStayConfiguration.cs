using Domain.HospitalizationStays.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.HospitalizationStays.Configuration;

public sealed class HospitalizationStayConfiguration : IEntityTypeConfiguration<HospitalizationStay>
{
    public void Configure(EntityTypeBuilder<HospitalizationStay> builder)
    {
        builder.ToTable("HOSPITALIZATION_STAYS");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.ClientPetId)
            .HasColumnName("CLIENT_PET_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasOne(x => x.ClientPet)
            .WithMany()
            .HasForeignKey(x => x.ClientPetId)
            .HasConstraintName("FK_HOSPITALIZATION_STAYS_CLIENT_PET")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.AppointmentId)
            .HasColumnName("APPOINTMENT_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.HasValue ? guid.Value.ToString() : null,
                value => string.IsNullOrWhiteSpace(value) ? null : Guid.Parse(value))
            .IsRequired(false);

        builder.Property(x => x.AdmittedByUserId)
            .HasColumnName("ADMITTED_BY_USER_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(x => x.FechaIngreso)
            .HasColumnName("FECHA_INGRESO")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.FechaAlta)
            .HasColumnName("FECHA_ALTA")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);

        builder.Property(x => x.Estado)
            .HasColumnName("ESTADO")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Motivo)
            .HasColumnName("MOTIVO")
            .HasColumnType("VARCHAR2(500)")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.DailyRate)
            .HasColumnName("DAILY_RATE")
            .HasColumnType("DECIMAL(18,2)")
            .IsRequired();

        builder.Property(x => x.IsPaid)
            .HasColumnName("IS_PAID")
            .HasConversion<int>()
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.PaidAt)
            .HasColumnName("PAID_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATE_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);

        // No es único: solo acelera GetActiveByPetIdAsync. La regla "una sola estancia
        // activa por mascota" la garantiza, además del handler, el índice único funcional
        // UX_HOSP_STAY_ACTIVE_PER_PET creado con SQL en la migración (EF no puede modelarlo;
        // Oracle excluye los NULL de la unicidad, así que las altas no cuentan):
        //   CREATE UNIQUE INDEX UX_HOSP_STAY_ACTIVE_PER_PET
        //     ON HOSPITALIZATION_STAYS (CASE WHEN ESTADO = 0 THEN CLIENT_PET_ID END)
        // Su violación (ORA-00001) se traduce a 409 en OracleHospitalizationStayConflictMapper.
        builder.HasIndex(x => new { x.ClientPetId, x.Estado })
            .HasDatabaseName("IX_HOSP_STAY_ACTIVE_PER_PET");
    }
}
