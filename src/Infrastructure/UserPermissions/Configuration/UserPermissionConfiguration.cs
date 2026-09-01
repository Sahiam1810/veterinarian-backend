using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModuleEntity = Domain.Modules.Entities.ModuleEntity;
using UserEntity = Domain.Users.Entities.Users;
using UserPermissionEntity = Domain.UserPermissions.Entities.UserPermission;

namespace Infrastructure.UserPermissions.Configuration;

public sealed class UserPermissionConfiguration
    : IEntityTypeConfiguration<UserPermissionEntity>
{
    public void Configure(EntityTypeBuilder<UserPermissionEntity> builder)
    {
        builder.ToTable("USER_PERMISSIONS");

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Id)
            .HasColumnName("USER_PERMISSION_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid  => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(permission => permission.UserId)
            .HasColumnName("USER_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid  => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(permission => permission.ModuleId)
            .HasColumnName("MODULE_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid  => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(permission => permission.CanView)
            .HasColumnName("CAN_VIEW")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(permission => permission.CanCreate)
            .HasColumnName("CAN_CREATE")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(permission => permission.CanEdit)
            .HasColumnName("CAN_EDIT")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(permission => permission.CanDelete)
            .HasColumnName("CAN_DELETE")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(permission => permission.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(permission => permission.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP");

        builder.HasIndex(permission => new { permission.UserId, permission.ModuleId })
            .IsUnique();

        builder.HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(permission => permission.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ModuleEntity>()
            .WithMany()
            .HasForeignKey(permission => permission.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
