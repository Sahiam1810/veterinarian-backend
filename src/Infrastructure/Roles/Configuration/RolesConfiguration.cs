using Domain.Common.Security;
using Domain.Roles.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoleEntity = Domain.Roles.Entities.Roles;

namespace Infrastructure.Roles.Configuration;

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

        // HasData espera los valores en el tipo CLR de la propiedad (Guid, RoleName),
        // no en el tipo ya convertido; EF aplica el HasConversion al generar la migración.
        var seededAt = new DateTime(2026, 8, 13, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new
            {
                Id = SystemRoles.AdminId,
                Name = RoleName.Create(SystemRoles.Admin),
                Description = "Administrador del sistema",
                CreatedAt = seededAt
            },
            new
            {
                Id = SystemRoles.VeterinarianId,
                Name = RoleName.Create(SystemRoles.Veterinarian),
                Description = "Consulta su agenda, atiende citas y registra la historia clínica de la mascota",
                CreatedAt = seededAt
            },
            new
            {
                Id = SystemRoles.ReceptionistId,
                Name = RoleName.Create(SystemRoles.Receptionist),
                Description = "Registra dueños y mascotas, agenda, reprograma y cancela citas",
                CreatedAt = seededAt
            },
            new
            {
                Id = SystemRoles.AssistantId,
                Name = RoleName.Create(SystemRoles.Assistant),
                Description = "Apoya el registro y la preparación de la atención",
                CreatedAt = seededAt
            },
            new
            {
                Id = SystemRoles.ClientId,
                Name = RoleName.Create(SystemRoles.Client),
                Description = "Portal para ver sus mascotas y sus citas",
                CreatedAt = seededAt
            });
    }
}
