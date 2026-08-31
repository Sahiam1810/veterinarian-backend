using Domain.Telegram.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserEntity = Domain.Users.Entities.Users;

namespace Infrastructure.Telegram.Configuration;

public sealed class TelegramLinkingSessionConfiguration
    : IEntityTypeConfiguration<TelegramLinkingSession>
{
    public void Configure(EntityTypeBuilder<TelegramLinkingSession> builder)
    {
        builder.ToTable("TELEGRAM_LINKING_SESSIONS");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).HasColumnName("ID").HasColumnType("VARCHAR2(36)")
            .HasConversion(value => value.ToString(), value => Guid.Parse(value)).ValueGeneratedNever();
        builder.Property(session => session.TelegramUserId).HasColumnName("TELEGRAM_USER_ID")
            .HasColumnType("NUMBER(19)").IsRequired();
        builder.Property(session => session.TelegramChatId).HasColumnName("TELEGRAM_CHAT_ID")
            .HasColumnType("NUMBER(19)").IsRequired();
        builder.Property(session => session.PersonId).HasColumnName("PERSON_ID").HasColumnType("VARCHAR2(36)")
            .HasConversion(
                value => value.HasValue ? value.Value.ToString() : null,
                value => string.IsNullOrWhiteSpace(value) ? null : Guid.Parse(value));
        builder.Property(session => session.EmailHash).HasColumnName("EMAIL_HASH")
            .HasColumnType("VARCHAR2(64)").HasMaxLength(64);
        builder.Property(session => session.OtpHash).HasColumnName("OTP_HASH")
            .HasColumnType("VARCHAR2(64)").HasMaxLength(64);
        builder.Property(session => session.Status).HasColumnName("STATUS")
            .HasColumnType("VARCHAR2(20)").HasMaxLength(20).HasConversion<string>().IsRequired();
        builder.Property(session => session.Attempts).HasColumnName("ATTEMPTS")
            .HasColumnType("NUMBER(10)").IsRequired();
        builder.Property(session => session.ExpiresAt).HasColumnName("EXPIRES_AT").HasColumnType("TIMESTAMP");
        builder.Property(session => session.CreatedAt).HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(session => session.UpdatedAt).HasColumnName("UPDATED_AT").HasColumnType("TIMESTAMP");
        builder.HasIndex(session => new { session.TelegramUserId, session.Status })
            .HasDatabaseName("IX_TELEGRAM_LINKING_SESSIONS_ACTIVE");
        builder.HasIndex(session => session.EmailHash)
            .HasDatabaseName("IX_TELEGRAM_LINKING_SESSIONS_EMAIL");
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(session => session.PersonId)
            .HasConstraintName("FK_TELEGRAM_LINKING_SESSIONS_USERS").OnDelete(DeleteBehavior.Restrict);
    }
}
