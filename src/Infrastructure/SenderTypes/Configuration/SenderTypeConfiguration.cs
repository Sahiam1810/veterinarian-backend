using Domain.SenderTypes.Entities;
using Domain.SenderTypes.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.SenderTypes.Configuration;

public sealed class SenderTypeConfiguration : IEntityTypeConfiguration<SenderTypeEntity>
{
    public void Configure(EntityTypeBuilder<SenderTypeEntity> builder)
    {
        builder.ToTable("SENDER_TYPES");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("SENDER_TYPES_ID").HasColumnType("VARCHAR2(36)")
            .HasConversion(guid => guid.ToString(), value => Guid.Parse(value)).IsRequired().ValueGeneratedNever();
        builder.Property(x => x.Name).HasColumnName("NAME_TYPE").HasColumnType("VARCHAR2(50)").HasMaxLength(SenderTypeName.MaxLength)
            .HasConversion(name => name.Value, value => SenderTypeName.Create(value)).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("CREATED_AT").HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("UPDATED_AT").HasColumnType("TIMESTAMP");
    }
}
