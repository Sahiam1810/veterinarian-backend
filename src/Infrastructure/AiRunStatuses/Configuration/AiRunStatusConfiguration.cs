using Domain.AiRunStatuses.Entities;
using Domain.AiRunStatuses.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.AiRunStatuses.Configuration;

public sealed class AiRunStatusConfiguration : IEntityTypeConfiguration<AiRunStatusEntity>
{
    public void Configure(EntityTypeBuilder<AiRunStatusEntity> builder)
    {
        builder.ToTable("AI_RUNS_STATUSES");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("AI_RUNS_STATUSES_ID").HasColumnType("VARCHAR2(36)")
            .HasConversion(guid => guid.ToString(), value => Guid.Parse(value)).IsRequired().ValueGeneratedNever();
        builder.Property(x => x.NameStatus).HasColumnName("NAME_STATUS").HasColumnType("VARCHAR2(50)").HasMaxLength(AiRunStatusName.MaxLength)
            .HasConversion(name => name.Value, value => AiRunStatusName.Create(value)).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CREATED_AT").HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UPDATED_AT").HasColumnType("TIMESTAMP");
    }
}
