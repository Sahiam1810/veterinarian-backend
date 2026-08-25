using HelpDesk.Domain.Common.Security;
using HelpDesk.Domain.Roles.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoleEntity = HelpDesk.Domain.Roles.Entities.Roles;

namespace HelpDesk.Infrastructure.Roles.Configuration;

public sealed class RolesConfiguration
    : IEntityTypeConfiguration<RoleEntity>
{
    public void Configure(EntityTypeBuilder<RoleEntity> builder)
    {
        builder.ToTable("ROLES");

        builder.HasKey(role => role.Id);

        // El provider de Oracle no mapea Guid nativamente a VARCHAR2(36);
        // por defecto intentaría usar RAW(16). Se fuerza la conversión
        // explícita guid → string para almacenarlo como texto en VARCHAR2(36).
        builder.Property(role => role.Id)
            .HasColumnName("ROLE_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid  => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(role => role.Name)
            .HasConversion(
                name  => name.Value,
                value => RoleName.Create(value))
            .HasColumnName("NAME")
            .HasColumnType("VARCHAR2(50)")
            .HasMaxLength(RoleName.MaxLength)
            .IsRequired();

        builder.Property(role => role.Description)
            .HasColumnName("DESCRIPTION")
            .HasColumnType("CLOB");

        builder.Property(role => role.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Ignore(role => role.UpdatedAt);

        builder.HasIndex(role => role.Name)
            .IsUnique();

        // El seed pasa el Id como string para que coincida con la conversión
        // aplicada en la propiedad (EF no aplica el HasConversion al seed data).
        var seededAt = new DateTime(2026, 8, 13, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new
            {
                Id = SystemRoles.AdminId.ToString(),
                Name = RoleName.Create(SystemRoles.Admin),
                Description = "System administrator",
                CreatedAt = seededAt
            },
            new
            {
                Id = SystemRoles.AgentId.ToString(),
                Name = RoleName.Create(SystemRoles.Agent),
                Description = "Help desk support agent",
                CreatedAt = seededAt
            },
            new
            {
                Id = SystemRoles.ClientId.ToString(),
                Name = RoleName.Create(SystemRoles.Client),
                Description = "Help desk client",
                CreatedAt = seededAt
            });
    }
}
