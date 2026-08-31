using Domain.ChatMessages.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatAttachmentEntity = Domain.ChatAttachments.Entities.ChatAttachment;

namespace Infrastructure.ChatAttachments.Configuration;

public sealed class ChatAttachmentConfiguration : IEntityTypeConfiguration<ChatAttachmentEntity>
{
    public void Configure(EntityTypeBuilder<ChatAttachmentEntity> builder)
    {
        builder.ToTable("CHAT_ATTACHMENTS");

        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.Id)
            .HasColumnName("CHAT_ATTACHMENTS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(attachment => attachment.ChatMessageId)
            .HasColumnName("CHAT_MESSAGES_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(attachment => attachment.ChatMessageId)
            .HasDatabaseName("IX_CHAT_ATTACHMENTS_CHAT_MESSAGES_ID");

        builder.HasOne<ChatMessage>()
            .WithMany()
            .HasForeignKey(attachment => attachment.ChatMessageId)
            .HasConstraintName("FK_CHAT_ATTACHMENTS_CHAT_MESSAGES_CHAT_MESSAGES_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(attachment => attachment.FileUrl)
            .HasColumnName("FILE_URL")
            .HasColumnType("VARCHAR2(1000)")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(attachment => attachment.FileType)
            .HasColumnName("FILE_TYPE")
            .HasColumnType("VARCHAR2(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(attachment => attachment.FileName)
            .HasColumnName("FILE_NAME")
            .HasColumnType("VARCHAR2(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(attachment => attachment.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Ignore(attachment => attachment.UpdatedAt);
    }
}
