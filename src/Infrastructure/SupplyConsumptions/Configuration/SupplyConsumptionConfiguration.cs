using Domain.HospitalizationStays.Entities;
using Domain.Supplies.Entities;
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
            .HasColumnType("VARCHAR2(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .IsRequired(false);

        builder.HasOne<Supply>()
            .WithMany()
            .HasForeignKey(x => x.SupplyId)
            .HasConstraintName("FK_SUPPLY_CONSUMPTIONS_SUPPLY")
            .OnDelete(DeleteBehavior.Restrict);

        // HospitalizationStay ya existe en develop (se mergeó antes que esta
        // rama se creara) -- el FK real se puede declarar de una vez, en vez
        // de dejarlo como seguimiento para cuando ambas ramas se juntaran.
        builder.HasOne<HospitalizationStay>()
            .WithMany()
            .HasForeignKey(x => x.HospitalizationStayId)
            .HasConstraintName("FK_SUPPLY_CONSUMPTIONS_STAY")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
