using Domain.ChatConversations.Entities;
using Domain.EscalationStatuses.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;

namespace Infrastructure.ChatEscalations.Configuration;

// Mapeo EF Core de CHAT_ESCALATIONS según el DDL Oracle.
public sealed class ChatEscalationConfiguration : IEntityTypeConfiguration<ChatEscalationEntity>
{
    public void Configure(EntityTypeBuilder<ChatEscalationEntity> builder)
    {
        builder.ToTable("CHAT_ESCALATIONS");

        builder.HasKey(escalation => escalation.Id);

        builder.Property(escalation => escalation.Id)
            .HasColumnName("CHAT_ESCALATIONS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(escalation => escalation.ChatConversationId)
            .HasColumnName("CHAT_CONVERSATIONS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(escalation => escalation.ChatConversationId)
            .HasDatabaseName("IX_CHAT_ESC_CONV_ID");

        builder.HasOne<ChatConversation>()
            .WithMany()
            .HasForeignKey(escalation => escalation.ChatConversationId)
            .HasConstraintName("FK_CHAT_ESC_CONV_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(escalation => escalation.EscalationStatusId)
            .HasColumnName("ESCALATIONS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(escalation => escalation.EscalationStatusId)
            .HasDatabaseName("IX_CHAT_ESC_STAT_ID");

        builder.HasOne<EscalationStatusEntity>()
            .WithMany()
            .HasForeignKey(escalation => escalation.EscalationStatusId)
            .HasConstraintName("FK_CHAT_ESC_STAT_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(escalation => escalation.FromAi)
            .HasColumnName("FROM_AI")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(escalation => escalation.Reason)
            .HasColumnName("REASON")
            .HasColumnType("CLOB")
            .IsRequired(false);

        builder.Property(escalation => escalation.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(escalation => escalation.UpdateAt)
            .HasColumnName("UPDATE_AT")
            .HasColumnType("VARCHAR2(36)")
            .IsRequired(false);
    }
}
