using Domain.ChatConversations.Entities;
using Domain.ChatParticipants.Entities;
using Domain.MessageTypes.Entities;
using Domain.SenderTypes.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatMessageEntity = Domain.ChatMessages.Entities.ChatMessage;

namespace Infrastructure.ChatMessages.Configuration;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessageEntity>
{
    public void Configure(EntityTypeBuilder<ChatMessageEntity> builder)
    {
        builder.ToTable("CHAT_MESSAGES");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("CHAT_MESSAGES_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(message => message.ChatConversationId)
            .HasColumnName("CHAT_CONVERSATIONS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(message => message.ChatConversationId)
            .HasDatabaseName("IX_CHAT_MESSAGES_CHAT_CONVERSATIONS_ID");

        builder.HasOne<ChatConversation>()
            .WithMany()
            .HasForeignKey(message => message.ChatConversationId)
            .HasConstraintName("FK_CHAT_MESSAGES_CHAT_CONVERSATIONS_CHAT_CONVERSATIONS_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(message => message.SenderTypesId)
            .HasColumnName("SENDER_TYPES_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(message => message.SenderTypesId)
            .HasDatabaseName("IX_CHAT_MESSAGES_SENDER_TYPES_ID");

        builder.HasOne<SenderTypeEntity>()
            .WithMany()
            .HasForeignKey(message => message.SenderTypesId)
            .HasConstraintName("FK_CHAT_MESSAGES_SENDER_TYPES_SENDER_TYPES_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(message => message.MessageTypeId)
            .HasColumnName("MESSAGE_TYPE_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(message => message.MessageTypeId)
            .HasDatabaseName("IX_CHAT_MESSAGES_MESSAGE_TYPE_ID");

        builder.HasOne<MessageTypeEntity>()
            .WithMany()
            .HasForeignKey(message => message.MessageTypeId)
            .HasConstraintName("FK_CHAT_MESSAGES_MESSAGE_TYPES_MESSAGE_TYPE_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(message => message.ChatParticipantId)
            .HasColumnName("CHAT_PARTICIPANTS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(message => message.ChatParticipantId)
            .HasDatabaseName("IX_CHAT_MESSAGES_CHAT_PARTICIPANTS_ID");

        builder.HasOne<ChatParticipant>()
            .WithMany()
            .HasForeignKey(message => message.ChatParticipantId)
            .HasConstraintName("FK_CHAT_MESSAGES_CHAT_PARTICIPANTS_CHAT_PARTICIPANTS_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(message => message.Content)
            .HasColumnName("CONTENT")
            .HasColumnType("CLOB")
            .IsRequired();

        builder.Property(message => message.Metadata)
            .HasColumnName("METADATA")
            .HasColumnType("CLOB")
            .IsRequired(false);

        builder.Property(message => message.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Ignore(message => message.UpdatedAt);
    }
}
