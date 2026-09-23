using Domain.SupplyConsumptions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.SupplyConsumptions.Configuration;

public class SupplyConsumptionConfiguration : IEntityTypeConfiguration<SupplyConsumption>
{
    public void Configure(EntityTypeBuilder<SupplyConsumption> builder)
    {
        builder.ToTable("SUPPLY_CONSUMPTIONS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("SUPPLY_CONSUMPTION_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.HospitalizationStayId)
            .HasColumnName("HOSPITALIZATION_STAY_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        builder.Property(x => x.SupplyId)
            .HasColumnName("SUPPLY_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("QUANTITY")
            .HasColumnType("NUMBER")
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasColumnName("UNIT_PRICE")
            .HasColumnType("NUMBER")
            .IsRequired();

        builder.Property(x => x.Total)
            .HasColumnName("TOTAL")
            .HasColumnType("NUMBER")
            .IsRequired();

        builder.Property(x => x.RegisteredByUserId)
            .HasColumnName("REGISTERED_BY_USER_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasColumnName("NOTES")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT")
            .IsRequired();
    }
}
