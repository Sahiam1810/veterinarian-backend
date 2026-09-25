using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserEntity = Domain.Users.Entities.Users;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Infrastructure.UserTokens.Configuration;

public sealed class UserTokensConfiguration
    : IEntityTypeConfiguration<UserTokenEntity>
{
    public void Configure(EntityTypeBuilder<UserTokenEntity> builder)
    {
        builder.ToTable("USER_TOKENS");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Id)
            .HasColumnName("TOKEN_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid  => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(token => token.UserId)
            .HasColumnName("USER_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid  => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(token => token.TokenValue)
            .HasColumnName("TOKEN_VALUE")
            .HasColumnType("VARCHAR2(500)")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(token => token.TokenType)
            .HasColumnName("TOKEN_TYPE")
            .HasColumnType("VARCHAR2(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(token => token.ExpiresAt)
            .HasColumnName("EXPIRES_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(token => token.SessionStartedAt)
            .HasColumnName("SESSION_STARTED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(token => token.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(token => token.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP");

        builder.HasIndex(token => token.TokenValue)
            .IsUnique();

        builder.HasIndex(token => token.UserId);

        builder.HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
