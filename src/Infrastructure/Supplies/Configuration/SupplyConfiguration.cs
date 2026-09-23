using Domain.Supplies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Supplies.Configuration;

public class SupplyConfiguration : IEntityTypeConfiguration<Supply>
{
    public void Configure(EntityTypeBuilder<Supply> builder)
    {
        builder.ToTable("SUPPLIES");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("SUPPLY_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("NAME")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Unit)
            .HasColumnName("UNIT")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasColumnName("UNIT_PRICE")
            .HasColumnType("NUMBER")
            .IsRequired();

        builder.Property(x => x.Stock)
            .HasColumnName("STOCK")
            .HasColumnType("NUMBER")
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("IS_ACTIVE")
            .HasConversion<int>()
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .IsRequired(false);
    }
}
