using Domain.ConversationStatuses.Entities;
using Domain.Priorities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatConversationEntity = Domain.ChatConversations.Entities.ChatConversation;

namespace Infrastructure.ChatConversations.Configuration;

public sealed class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversationEntity>
{
    public void Configure(EntityTypeBuilder<ChatConversationEntity> builder)
    {
        builder.ToTable("CHAT_CONVERSATIONS");

        builder.HasKey(conversation => conversation.Id);

        // El provider de Oracle no mapea Guid nativamente a VARCHAR2(36);
        // por defecto intentaría usar RAW(16). Se fuerza la conversión
        // explícita guid → string para almacenarlo como texto en VARCHAR2(36).
        builder.Property(conversation => conversation.Id)
            .HasColumnName("CHAT_CONVERSATIONS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(conversation => conversation.ConversationStatusId)
            .HasColumnName("CONVERSATION_STATUS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(conversation => conversation.ConversationStatusId)
            .HasDatabaseName("IX_CHAT_CONVERSATIONS_CONVERSATION_STATUS_ID");

        builder.HasOne<ConversationStatusEntity>()
            .WithMany()
            .HasForeignKey(conversation => conversation.ConversationStatusId)
            .HasConstraintName("FK_CHAT_CONVERSATIONS_CONVERSATIONS_STATUSES_CONVERSATION_STATUS_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(conversation => conversation.PriorityId)
            .HasColumnName("PRIORITY_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.HasValue ? guid.Value.ToString() : null,
                value => value == null ? null : Guid.Parse(value))
            .IsRequired(false);

        builder.HasIndex(conversation => conversation.PriorityId)
            .HasDatabaseName("IX_CHAT_CONVERSATIONS_PRIORITY_ID");

        builder.HasOne<PriorityEntity>()
            .WithMany()
            .HasForeignKey(conversation => conversation.PriorityId)
            .HasConstraintName("FK_CHAT_CONVERSATIONS_PRIORITY_PRIORITY_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(conversation => conversation.AiEnabled)
            .HasColumnName("AI_ENABLED")
            .HasConversion<int>()
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(conversation => conversation.LastMessageAt)
            .HasColumnName("LAST_MESSAGE_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);

        builder.Property(conversation => conversation.Closed)
            .HasColumnName("CLOSED")
            .HasConversion<int>()
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(conversation => conversation.ClosedAt)
            .HasColumnName("CLOSED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);

        builder.Property(conversation => conversation.ClosedBy)
            .HasColumnName("CLOSED_BY")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.HasValue ? guid.Value.ToString() : null,
                value => value == null ? null : Guid.Parse(value))
            .IsRequired(false);

        builder.Property(conversation => conversation.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(conversation => conversation.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);
    }
}
