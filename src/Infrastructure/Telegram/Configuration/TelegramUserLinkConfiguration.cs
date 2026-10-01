using Domain.Clients.Entities;
using Domain.Telegram.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Telegram.Configuration;

public sealed class TelegramUserLinkConfiguration
    : IEntityTypeConfiguration<TelegramUserLink>
{
    public void Configure(EntityTypeBuilder<TelegramUserLink> builder)
    {
        builder.ToTable("TELEGRAM_USER_LINKS");
        builder.HasKey(link => link.Id);
        builder.Property(link => link.Id).HasColumnName("ID").HasColumnType("VARCHAR2(36)")
            .HasConversion(value => value.ToString(), value => Guid.Parse(value)).ValueGeneratedNever();
        builder.Property(link => link.ClientId).HasColumnName("CLIENT_ID").HasColumnType("VARCHAR2(36)")
            .HasConversion(value => value.ToString(), value => Guid.Parse(value)).IsRequired();
        builder.Property(link => link.TelegramUserId).HasColumnName("TELEGRAM_USER_ID").HasColumnType("NUMBER(19)").IsRequired();
        builder.Property(link => link.TelegramChatId).HasColumnName("TELEGRAM_CHAT_ID").HasColumnType("NUMBER(19)").IsRequired();
        builder.Property(link => link.ChatConversationId).HasColumnName("CHAT_CONVERSATION_ID").HasColumnType("VARCHAR2(36)")
            .HasConversion(guid => guid.HasValue ? guid.Value.ToString() : null, value => !string.IsNullOrEmpty(value) ? Guid.Parse(value) : null);
        builder.Property(link => link.LinkedAt).HasColumnName("LINKED_AT").HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(link => link.UnlinkedAt).HasColumnName("UNLINKED_AT").HasColumnType("TIMESTAMP");
        builder.Property(link => link.CreatedAt).HasColumnName("CREATED_AT").HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(link => link.UpdatedAt).HasColumnName("UPDATED_AT").HasColumnType("TIMESTAMP");
        builder.HasOne<ClientEntity>().WithMany().HasForeignKey(link => link.ClientId)
            .HasConstraintName("FK_TELEGRAM_USER_LINKS_CLIENTS").OnDelete(DeleteBehavior.Restrict);
    }
}
