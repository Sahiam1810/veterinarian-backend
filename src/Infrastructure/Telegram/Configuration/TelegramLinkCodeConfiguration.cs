using Domain.Telegram.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserEntity = Domain.Users.Entities.Users;

namespace Infrastructure.Telegram.Configuration;

public sealed class TelegramLinkCodeConfiguration
    : IEntityTypeConfiguration<TelegramLinkCode>
{
    public void Configure(EntityTypeBuilder<TelegramLinkCode> builder)
    {
        builder.ToTable("TELEGRAM_LINK_CODES");
        builder.HasKey(code => code.Id);
        builder.Property(code => code.Id).HasColumnName("ID").HasColumnType("VARCHAR2(36)")
            .HasConversion(value => value.ToString(), value => Guid.Parse(value)).ValueGeneratedNever();
        builder.Property(code => code.PersonId).HasColumnName("PERSON_ID").HasColumnType("VARCHAR2(36)")
            .HasConversion(value => value.ToString(), value => Guid.Parse(value)).IsRequired();
        builder.Property(code => code.CodeHash).HasColumnName("CODE_HASH").HasColumnType("VARCHAR2(64)")
            .HasMaxLength(64).IsRequired();
        builder.Property(code => code.ExpiresAt).HasColumnName("EXPIRES_AT").HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(code => code.ConsumedAt).HasColumnName("CONSUMED_AT").HasColumnType("TIMESTAMP");
        builder.Property(code => code.InvalidatedAt).HasColumnName("INVALIDATED_AT").HasColumnType("TIMESTAMP");
        builder.Property(code => code.CreatedAt).HasColumnName("CREATED_AT").HasColumnType("TIMESTAMP").IsRequired();
        builder.Property(code => code.UpdatedAt).HasColumnName("UPDATED_AT").HasColumnType("TIMESTAMP");
        builder.HasIndex(code => code.CodeHash).IsUnique().HasDatabaseName("UX_TELEGRAM_LINK_CODES_HASH");
        builder.HasIndex(code => code.PersonId).HasDatabaseName("IX_TELEGRAM_LINK_CODES_PERSON");
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(code => code.PersonId)
            .HasConstraintName("FK_TELEGRAM_LINK_CODES_USERS").OnDelete(DeleteBehavior.Restrict);
    }
}
