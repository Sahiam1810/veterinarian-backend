using Domain.EscalationStatuses.Entities;
using Domain.EscalationStatuses.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EscalationStatuses.Configuration;

// Mapeo EF Core de escalations_statuses según el DDL Oracle.
public sealed class EscalationStatusConfiguration : IEntityTypeConfiguration<EscalationStatusEntity>
{
    public void Configure(EntityTypeBuilder<EscalationStatusEntity> builder)
    {
        builder.ToTable("ESCALATIONS_STATUSES");

        builder.HasKey(x => x.Id);

        // PK según DDL: escalations_id
        builder.Property(x => x.Id)
            .HasColumnName("ESCALATIONS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(guid => guid.ToString(), value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasColumnName("NAME_STATUS")
            .HasColumnType("VARCHAR2(50)")
            .HasMaxLength(EscalationStatusName.MaxLength)
            .HasConversion(name => name.Value, value => EscalationStatusName.Create(value))
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP");
    }
}
