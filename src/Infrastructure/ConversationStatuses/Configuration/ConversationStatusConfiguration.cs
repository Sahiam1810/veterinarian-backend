using Domain.ConversationStatuses.Entities;
using Domain.ConversationStatuses.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.ConversationStatuses.Configuration;

// Mapeo EF Core de conversations_statuses según el DDL Oracle.
public sealed class ConversationStatusConfiguration : IEntityTypeConfiguration<ConversationStatusEntity>
{
    public void Configure(EntityTypeBuilder<ConversationStatusEntity> builder)
    {
        builder.ToTable("CONVERSATIONS_STATUSES");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("CONVERSATIONS_STATUSES_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(guid => guid.ToString(), value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasColumnName("NAME_STATUS")
            .HasColumnType("VARCHAR2(50)")
            .HasMaxLength(ConversationStatusName.MaxLength)
            .HasConversion(name => name.Value, value => ConversationStatusName.Create(value))
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
