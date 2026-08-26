using Domain.UserAccounts.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserEntity = Domain.Users.Entities.Users;

namespace Infrastructure.UserAccounts.Configuration;

public sealed class UserAccountsConfiguration
    : IEntityTypeConfiguration<UserAccountEntity>
{
    public void Configure(EntityTypeBuilder<UserAccountEntity> builder)
    {
        builder.ToTable("USER_ACCOUNTS");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id)
            .HasColumnName("ACCOUNT_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid  => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(account => account.UserId)
            .HasColumnName("USER_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid  => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(account => account.Username)
            .HasConversion(
                username => username.Value,
                value    => AccountUsername.Create(value))
            .HasColumnName("USERNAME")
            .HasColumnType("VARCHAR2(30)")
            .HasMaxLength(AccountUsername.MaxLength)
            .IsRequired();

        builder.Property(account => account.Mail)
            .HasConversion(
                mail  => mail.Value,
                value => AccountMail.Create(value))
            .HasColumnName("MAIL")
            .HasColumnType("VARCHAR2(150)")
            .HasMaxLength(AccountMail.MaxLength)
            .IsRequired();

        builder.Property(account => account.Status)
            .HasColumnName("STATUS")
            .HasColumnType("VARCHAR2(40)")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(account => account.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(account => account.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP");

        builder.HasIndex(account => account.Username)
            .IsUnique();

        builder.HasIndex(account => account.UserId)
            .IsUnique();

        builder.HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(account => account.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
