using Domain.Telegram.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserEntity = Domain.Users.Entities.Users;

namespace Infrastructure.Telegram.Configuration;

public sealed class TelegramIdentitySessionConfiguration
    : IEntityTypeConfiguration<TelegramIdentitySession>
{
    public void Configure(EntityTypeBuilder<TelegramIdentitySession> builder)
    {
        builder.ToTable("TELEGRAM_IDENTITY_SESSIONS");
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
        builder.Property(session => session.ProtectedIdentification).HasColumnName("PROTECTED_IDENTIFICATION")
            .HasColumnType("VARCHAR2(512)").HasMaxLength(512);
        builder.Property(session => session.ProtectedFullName).HasColumnName("PROTECTED_FULL_NAME")
            .HasColumnType("VARCHAR2(1024)").HasMaxLength(1024);
        builder.Property(session => session.ProtectedEmail).HasColumnName("PROTECTED_EMAIL")
            .HasColumnType("VARCHAR2(1024)").HasMaxLength(1024);
        builder.Property(session => session.ProtectedPendingMessage).HasColumnName("PROTECTED_PENDING_MESSAGE")
            .HasColumnType("CLOB");
        builder.Property(session => session.OtpHash).HasColumnName("OTP_HASH")
            .HasColumnType("VARCHAR2(64)").HasMaxLength(64);
        builder.Property(session => session.Status).HasColumnName("STATUS")
            .HasColumnType("VARCHAR2(40)").HasMaxLength(40).HasConversion<string>().IsRequired();
        builder.Property(session => session.OtpAttempts).HasColumnName("OTP_ATTEMPTS")
            .HasColumnType("NUMBER(10)").IsRequired();
        builder.Property(session => session.OtpExpiresAt).HasColumnName("OTP_EXPIRES_AT")
            .HasColumnType("TIMESTAMP");
        builder.Property(session => session.AbsoluteExpiresAt).HasColumnName("ABSOLUTE_EXPIRES_AT")
            .HasColumnType("TIMESTAMP");
        builder.Property(session => session.IdleExpiresAt).HasColumnName("IDLE_EXPIRES_AT")
            .HasColumnType("TIMESTAMP");
        builder.Property(session => session.PendingInboundUpdateId).HasColumnName("PENDING_INBOUND_UPDATE_ID")
            .HasColumnType("NUMBER(19)");
        builder.Property(session => session.CreatedAt).HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(session => session.UpdatedAt).HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP");
        builder.HasIndex(session => new { session.TelegramUserId, session.Status })
            .HasDatabaseName("IX_TG_ID_SESSIONS_USER_STATUS");
        builder.HasIndex(session => session.TelegramChatId)
            .HasDatabaseName("IX_TG_ID_SESSIONS_CHAT");
        builder.HasIndex(session => session.PendingInboundUpdateId)
            .IsUnique()
            .HasDatabaseName("UX_TG_ID_SESSIONS_PENDING");
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(session => session.PersonId)
            .HasConstraintName("FK_TG_ID_SESSIONS_USERS").OnDelete(DeleteBehavior.Restrict);
    }
}
