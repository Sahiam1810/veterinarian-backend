using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModuleEntity = Domain.Modules.Entities.ModuleEntity;
using RoleEntity = Domain.Roles.Entities.Roles;
using RolePermissionEntity = Domain.RolePermissions.Entities.RolePermission;

namespace Infrastructure.RolePermissions.Configuration;

public sealed class RolePermissionConfiguration
    : IEntityTypeConfiguration<RolePermissionEntity>
{
    public void Configure(EntityTypeBuilder<RolePermissionEntity> builder)
    {
        builder.ToTable("ROLE_PERMISSIONS");

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Id)
            .HasColumnName("ROLE_PERMISSION_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid  => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(permission => permission.RoleId)
            .HasColumnName("ROLE_ID")
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

        builder.HasIndex(permission => new { permission.RoleId, permission.ModuleId })
            .IsUnique();

        builder.HasOne<RoleEntity>()
            .WithMany()
            .HasForeignKey(permission => permission.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ModuleEntity>()
            .WithMany()
            .HasForeignKey(permission => permission.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
