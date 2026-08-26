using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;
using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;

namespace Infrastructure.UserCredentials.Configuration;

public sealed class UserCredentialsConfiguration
    : IEntityTypeConfiguration<UserCredentialsEntity>
{
    public void Configure(EntityTypeBuilder<UserCredentialsEntity> builder)
    {
        builder.ToTable("USER_CREDENTIALS");

        builder.HasKey(credentials => credentials.Id);

        builder.Property(credentials => credentials.Id)
            .HasColumnName("CREDENTIAL_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid  => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(credentials => credentials.AccountId)
            .HasColumnName("ACCOUNT_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid  => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(credentials => credentials.PasswordHash)
            .HasColumnName("PASSWORD_HASH")
            .HasColumnType("VARCHAR2(255)")
            .IsRequired();

        builder.Property(credentials => credentials.LastChanged)
            .HasColumnName("LAST_CHANGED")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(credentials => credentials.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(credentials => credentials.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP");

        builder.HasIndex(credentials => credentials.AccountId)
            .IsUnique();

        builder.HasOne<UserAccountEntity>()
            .WithMany()
            .HasForeignKey(credentials => credentials.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
