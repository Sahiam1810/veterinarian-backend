using Domain.ChatEscalations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatEscalationResolutionEntity = Domain.ChatEscalationResolutions.Entities.ChatEscalationResolution;

namespace Infrastructure.ChatEscalationResolutions.Configuration;

// Mapeo EF Core de CHAT_ESCALATION_RESOLUTION según el DDL Oracle.
public sealed class ChatEscalationResolutionConfiguration
    : IEntityTypeConfiguration<ChatEscalationResolutionEntity>
{
    public void Configure(EntityTypeBuilder<ChatEscalationResolutionEntity> builder)
    {
        builder.ToTable("CHAT_ESCALATION_RESOLUTION");

        builder.HasKey(resolution => resolution.Id);

        builder.Property(resolution => resolution.Id)
            .HasColumnName("CHAT_ESCALATION_RESOLUTION_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(resolution => resolution.ChatEscalationId)
            .HasColumnName("CHAT_ESCALATIONS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(resolution => resolution.ChatEscalationId)
            .HasDatabaseName("IX_CHAT_ESC_RES_ESC");

        builder.HasOne<ChatEscalation>()
            .WithMany()
            .HasForeignKey(resolution => resolution.ChatEscalationId)
            .HasConstraintName("FK_CHAT_ESC_RES_ESC")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(resolution => resolution.ResolvedBy)
            .HasColumnName("RESOLVED_BY")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.HasValue ? guid.Value.ToString() : null,
                value => value == null ? null : Guid.Parse(value))
            .IsRequired(false);

        builder.Property(resolution => resolution.ResolutionNote)
            .HasColumnName("RESOLUTION_NOTE")
            .HasColumnType("CLOB")
            .IsRequired(false);

        builder.Property(resolution => resolution.ResolvedAt)
            .HasColumnName("RESOLVED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);
    }
}
