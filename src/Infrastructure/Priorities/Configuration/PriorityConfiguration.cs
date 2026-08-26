using Domain.Priorities.Entities;
using Domain.Priorities.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Priorities.Configuration;

// Mapeo EF Core de priority según el DDL Oracle.
public sealed class PriorityConfiguration : IEntityTypeConfiguration<PriorityEntity>
{
    public void Configure(EntityTypeBuilder<PriorityEntity> builder)
    {
        builder.ToTable("PRIORITY");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("PRIORITY_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(guid => guid.ToString(), value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasColumnName("NAME_PRIORITY")
            .HasColumnType("VARCHAR2(50)")
            .HasMaxLength(PriorityName.MaxLength)
            .HasConversion(name => name.Value, value => PriorityName.Create(value))
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
