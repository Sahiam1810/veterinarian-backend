using Domain.ProcedureOrders.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.ProcedureOrders.Configuration;

public class ProcedureOrderItemConfiguration : IEntityTypeConfiguration<ProcedureOrderItem>
{
    public void Configure(EntityTypeBuilder<ProcedureOrderItem> builder)
    {
        builder.ToTable("PROCEDURE_ORDER_ITEMS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("PROCEDURE_ORDER_ITEM_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.ProcedureOrderId)
            .HasColumnName("PROCEDURE_ORDER_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        builder.Property(x => x.ProcedureId)
            .HasColumnName("PROCEDURE_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasColumnName("UNIT_PRICE")
            .HasColumnType("NUMBER(12,2)")
            .IsRequired(false);

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

        builder.HasOne(x => x.Procedure)
            .WithMany()
            .HasForeignKey(x => x.ProcedureId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
