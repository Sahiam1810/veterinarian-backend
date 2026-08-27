using Domain.MessageTypes.Entities;
using Domain.MessageTypes.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MessageTypes.Configuration;

public sealed class MessageTypeConfiguration : IEntityTypeConfiguration<MessageTypeEntity>
{
    public void Configure(EntityTypeBuilder<MessageTypeEntity> builder)
    {
        builder.ToTable("MESSAGE_TYPES");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("MESSAGE_TYPES_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(guid => guid.ToString(), value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasColumnName("NAME_TYPE")
            .HasColumnType("VARCHAR2(50)")
            .HasMaxLength(MessageTypeName.MaxLength)
            .HasConversion(name => name.Value, value => MessageTypeName.Create(value))
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
