using Domain.Telegram.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserEntity = Domain.Users.Entities.Users;

namespace Infrastructure.Telegram.Configuration;

public sealed class TelegramRegistrationSessionConfiguration
    : IEntityTypeConfiguration<TelegramRegistrationSession>
{
    public void Configure(EntityTypeBuilder<TelegramRegistrationSession> builder)
    {
        builder.ToTable("TELEGRAM_REGISTRATION_SESSIONS");
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
        builder.Property(session => session.ProtectedEmail).HasColumnName("PROTECTED_EMAIL")
            .HasColumnType("VARCHAR2(2048)").HasMaxLength(2048);
        builder.Property(session => session.EmailHash).HasColumnName("EMAIL_HASH")
            .HasColumnType("VARCHAR2(64)").HasMaxLength(64);
        builder.Property(session => session.OtpHash).HasColumnName("OTP_HASH")
            .HasColumnType("VARCHAR2(64)").HasMaxLength(64);
        builder.Property(session => session.CompletionTokenHash).HasColumnName("COMPLETION_TOKEN_HASH")
            .HasColumnType("VARCHAR2(64)").HasMaxLength(64);
        builder.Property(session => session.AccountKind).HasColumnName("ACCOUNT_KIND")
            .HasColumnType("VARCHAR2(24)").HasMaxLength(24).HasConversion<string>().IsRequired();
        builder.Property(session => session.Status).HasColumnName("STATUS")
            .HasColumnType("VARCHAR2(24)").HasMaxLength(24).HasConversion<string>().IsRequired();
        builder.Property(session => session.Attempts).HasColumnName("ATTEMPTS")
            .HasColumnType("NUMBER(10)").IsRequired();
        builder.Property(session => session.OtpExpiresAt).HasColumnName("OTP_EXPIRES_AT")
            .HasColumnType("TIMESTAMP");
        builder.Property(session => session.CompletionExpiresAt).HasColumnName("COMPLETION_EXPIRES_AT")
            .HasColumnType("TIMESTAMP");
        builder.Property(session => session.CreatedAt).HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(session => session.UpdatedAt).HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP");

        builder.HasIndex(session => new { session.TelegramUserId, session.Status })
            .HasDatabaseName("IX_TG_REG_SESSIONS_ACTIVE");
        builder.HasIndex(session => session.EmailHash)
            .HasDatabaseName("IX_TG_REG_SESSIONS_EMAIL");
        builder.HasIndex(session => session.CompletionTokenHash)
            .IsUnique()
            .HasDatabaseName("UX_TG_REG_SESSIONS_TOKEN");
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(session => session.PersonId)
            .HasConstraintName("FK_TG_REG_SESSIONS_USERS").OnDelete(DeleteBehavior.Restrict);
    }
}
