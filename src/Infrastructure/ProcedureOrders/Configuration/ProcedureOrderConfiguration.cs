using Domain.ProcedureOrders.Entities;
using Domain.HospitalizationStays.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.ProcedureOrders.Configuration;

public class ProcedureOrderConfiguration : IEntityTypeConfiguration<ProcedureOrder>
{
    public void Configure(EntityTypeBuilder<ProcedureOrder> builder)
    {
        builder.ToTable("PROCEDURE_ORDERS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("PROCEDURE_ORDER_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.ClientPetId)
            .HasColumnName("CLIENT_PET_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        builder.Property(x => x.VeterinarianId)
            .HasColumnName("VETERINARIAN_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        builder.Property(x => x.AppointmentId)
            .HasColumnName("APPOINTMENT_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        builder.Property(x => x.HospitalizationStayId)
            .HasColumnName("HOSPITALIZATION_STAY_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.HasValue ? guid.Value.ToString() : null,
                value => string.IsNullOrWhiteSpace(value) ? null : Guid.Parse(value))
            .IsRequired(false);

        builder.HasOne<HospitalizationStay>()
            .WithMany()
            .HasForeignKey(x => x.HospitalizationStayId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.IsInHouse)
            .HasColumnName("IS_IN_HOUSE")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ReferredTo)
            .HasColumnName("REFERRED_TO")
            .HasColumnType("VARCHAR2(200)")
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(x => x.ReferralReason)
            .HasColumnName("REFERRAL_REASON")
            .HasColumnType("VARCHAR2(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasColumnName("STATUS")
            .HasColumnType("VARCHAR2(30)")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ResultFileUrl)
            .HasColumnName("RESULT_FILE_URL")
            .HasColumnType("VARCHAR2(1000)")
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .IsRequired(false);

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.ProcedureOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
