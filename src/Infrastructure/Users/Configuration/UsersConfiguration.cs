using Domain.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoleEntity = Domain.Roles.Entities.Roles;
using UserEntity = Domain.Users.Entities.Users;

namespace Infrastructure.Users.Configuration;

public sealed class UsersConfiguration
    : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("USERS");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("USER_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid  => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(user => user.FullName)
            .HasColumnName("FULL_NAME")
            .HasColumnType("VARCHAR2(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasConversion(
                email => email.Value,
                value => UserEmail.Create(value))
            .HasColumnName("EMAIL")
            .HasColumnType("VARCHAR2(150)")
            .HasMaxLength(UserEmail.MaxLength)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasColumnName("PASSWORD_HASH")
            .HasColumnType("VARCHAR2(255)")
            .IsRequired();

        builder.Property(user => user.RoleId)
            .HasColumnName("ROLE_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid  => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(user => user.IsActive)
            .HasColumnName("IS_ACTIVE")
            .HasConversion<int>()
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(user => user.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP");

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.HasOne<RoleEntity>()
            .WithMany()
            .HasForeignKey(user => user.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
