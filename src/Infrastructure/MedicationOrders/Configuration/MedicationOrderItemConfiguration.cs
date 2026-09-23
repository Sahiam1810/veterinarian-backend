using Domain.MedicationOrders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MedicationOrders.Configuration;

public class MedicationOrderItemConfiguration : IEntityTypeConfiguration<MedicationOrderItem>
{
    public void Configure(EntityTypeBuilder<MedicationOrderItem> builder)
    {
        builder.ToTable("MEDICATION_ORDER_ITEMS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("MEDICATION_ORDER_ITEM_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.MedicationOrderId)
            .HasColumnName("MEDICATION_ORDER_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        builder.Property(x => x.MedicationId)
            .HasColumnName("MEDICATION_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasColumnName("NOTES")
            .HasColumnType("VARCHAR2(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .IsRequired(false);

        builder.HasOne(x => x.Medication)
            .WithMany()
            .HasForeignKey(x => x.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
