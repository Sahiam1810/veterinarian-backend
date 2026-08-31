using Domain.ChatEscalations.Entities;
using Domain.EscalationStatuses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatEscalationStatusHistoryEntity = Domain.ChatEscalationStatusHistories.Entities.ChatEscalationStatusHistory;

namespace Infrastructure.ChatEscalationStatusHistories.Configuration;

// Mapeo EF Core de CHAT_ESCALATION_STATUS_HISTORY según el DDL Oracle.
public sealed class ChatEscalationStatusHistoryConfiguration
    : IEntityTypeConfiguration<ChatEscalationStatusHistoryEntity>
{
    public void Configure(EntityTypeBuilder<ChatEscalationStatusHistoryEntity> builder)
    {
        builder.ToTable("CHAT_ESCALATION_STATUS_HISTORY");

        builder.HasKey(history => history.Id)
            .HasName("PK_CHAT_ESC_STAT_HIST");

        builder.Property(history => history.Id)
            .HasColumnName("CHAT_ESCALATION_STATUS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(history => history.EscalationStatusId)
            .HasColumnName("ESCALATIONS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(history => history.EscalationStatusId)
            .HasDatabaseName("IX_CHAT_ESC_HIST_STA");

        builder.HasOne<EscalationStatusEntity>()
            .WithMany()
            .HasForeignKey(history => history.EscalationStatusId)
            .HasConstraintName("FK_CHAT_ESC_HIST_STA")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(history => history.ChatEscalationId)
            .HasColumnName("CHAT_ESCALATIONS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(history => history.ChatEscalationId)
            .HasDatabaseName("IX_CHAT_ESC_HIST_ESC");

        builder.HasOne<ChatEscalation>()
            .WithMany()
            .HasForeignKey(history => history.ChatEscalationId)
            .HasConstraintName("FK_CHAT_ESC_HIST_ESC")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(history => history.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(history => history.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);
    }
}
