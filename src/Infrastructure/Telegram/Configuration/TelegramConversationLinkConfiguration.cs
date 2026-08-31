using Domain.ChatConversations.Entities;
using Domain.Telegram.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Telegram.Configuration;

public sealed class TelegramConversationLinkConfiguration
    : IEntityTypeConfiguration<TelegramConversationLink>
{
    public void Configure(EntityTypeBuilder<TelegramConversationLink> builder)
    {
        builder.ToTable("TELEGRAM_CONVERSATION_LINKS");
        builder.HasKey(link => link.Id);
        builder.Property(link => link.Id).HasColumnName("ID").HasColumnType("VARCHAR2(36)")
            .HasConversion(value => value.ToString(), value => Guid.Parse(value)).ValueGeneratedNever();
        builder.Property(link => link.TelegramUserLinkId).HasColumnName("TELEGRAM_USER_LINK_ID").HasColumnType("VARCHAR2(36)")
            .HasConversion(value => value.ToString(), value => Guid.Parse(value)).IsRequired();
        builder.Property(link => link.ConversationId).HasColumnName("CONVERSATION_ID").HasColumnType("VARCHAR2(36)")
            .HasConversion(value => value.ToString(), value => Guid.Parse(value)).IsRequired();
        builder.Property(link => link.CreatedAt).HasColumnName("CREATED_AT").HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(link => link.UpdatedAt).HasColumnName("UPDATED_AT").HasColumnType("TIMESTAMP");
        builder.HasIndex(link => link.TelegramUserLinkId).IsUnique().HasDatabaseName("UX_TELEGRAM_CONVERSATION_LINKS_USER");
        builder.HasIndex(link => link.ConversationId).IsUnique().HasDatabaseName("UX_TELEGRAM_CONVERSATION_LINKS_CONVERSATION");
        builder.HasOne<TelegramUserLink>().WithMany().HasForeignKey(link => link.TelegramUserLinkId)
            .HasConstraintName("FK_TELEGRAM_CONVERSATION_LINKS_USER_LINK").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ChatConversation>().WithMany().HasForeignKey(link => link.ConversationId)
            .HasConstraintName("FK_TELEGRAM_CONVERSATION_LINKS_CONVERSATION").OnDelete(DeleteBehavior.Restrict);
    }
}
