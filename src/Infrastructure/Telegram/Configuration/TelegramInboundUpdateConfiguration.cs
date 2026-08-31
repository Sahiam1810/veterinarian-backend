using Domain.Telegram.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Telegram.Configuration;

public sealed class TelegramInboundUpdateConfiguration
    : IEntityTypeConfiguration<TelegramInboundUpdate>
{
    public void Configure(EntityTypeBuilder<TelegramInboundUpdate> builder)
    {
        builder.ToTable("TELEGRAM_INBOUND_UPDATES");
        builder.HasKey(update => update.Id);
        builder.Property(update => update.Id).HasColumnName("UPDATE_ID").HasColumnType("NUMBER(19)").ValueGeneratedNever();
        builder.Property(update => update.TelegramUserId).HasColumnName("TELEGRAM_USER_ID").HasColumnType("NUMBER(19)").IsRequired();
        builder.Property(update => update.TelegramChatId).HasColumnName("TELEGRAM_CHAT_ID").HasColumnType("NUMBER(19)").IsRequired();
        builder.Property(update => update.TelegramMessageId).HasColumnName("TELEGRAM_MESSAGE_ID").HasColumnType("NUMBER(19)").IsRequired();
        builder.Property(update => update.ChatType).HasColumnName("CHAT_TYPE").HasColumnType("VARCHAR2(30)").HasMaxLength(30).IsRequired();
        builder.Property(update => update.MessageText).HasColumnName("MESSAGE_TEXT").HasColumnType("CLOB");
        builder.Property(update => update.ResponseText).HasColumnName("RESPONSE_TEXT").HasColumnType("CLOB");
        builder.Property(update => update.Status).HasColumnName("STATUS").HasColumnType("VARCHAR2(20)").HasConversion<string>().IsRequired();
        builder.Property(update => update.Attempts).HasColumnName("ATTEMPTS").HasColumnType("NUMBER(10)").IsRequired();
        builder.Property(update => update.NextAttemptAt).HasColumnName("NEXT_ATTEMPT_AT").HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(update => update.LastSentChunkIndex).HasColumnName("LAST_SENT_CHUNK_INDEX").HasColumnType("NUMBER(10)").IsRequired();
        builder.Property(update => update.LastErrorCode).HasColumnName("LAST_ERROR_CODE").HasColumnType("VARCHAR2(120)").HasMaxLength(120);
        builder.Property(update => update.CreatedAt).HasColumnName("CREATED_AT").HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(update => update.UpdatedAt).HasColumnName("UPDATED_AT").HasColumnType("TIMESTAMP");
        builder.HasIndex(update => new { update.Status, update.NextAttemptAt })
            .HasDatabaseName("IX_TELEGRAM_INBOUND_UPDATES_PENDING");
    }
}
