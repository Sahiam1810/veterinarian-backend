using Domain.Telegram.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserEntity = Domain.Users.Entities.Users;

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
        builder.Property(link => link.PersonId).HasColumnName("PERSON_ID").HasColumnType("VARCHAR2(36)")
            .HasConversion(value => value.ToString(), value => Guid.Parse(value)).IsRequired();
        builder.Property(link => link.TelegramUserId).HasColumnName("TELEGRAM_USER_ID").HasColumnType("NUMBER(19)").IsRequired();
        builder.Property(link => link.TelegramChatId).HasColumnName("TELEGRAM_CHAT_ID").HasColumnType("NUMBER(19)").IsRequired();
        builder.Property(link => link.LinkedAt).HasColumnName("LINKED_AT").HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(link => link.CreatedAt).HasColumnName("CREATED_AT").HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(link => link.UpdatedAt).HasColumnName("UPDATED_AT").HasColumnType("TIMESTAMP");
        builder.HasIndex(link => link.PersonId).IsUnique().HasDatabaseName("UX_TELEGRAM_USER_LINKS_PERSON");
        builder.HasIndex(link => link.TelegramUserId).IsUnique().HasDatabaseName("UX_TELEGRAM_USER_LINKS_USER");
        builder.HasIndex(link => link.TelegramChatId).IsUnique().HasDatabaseName("UX_TELEGRAM_USER_LINKS_CHAT");
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(link => link.PersonId)
            .HasConstraintName("FK_TELEGRAM_USER_LINKS_USERS").OnDelete(DeleteBehavior.Restrict);
    }
}
