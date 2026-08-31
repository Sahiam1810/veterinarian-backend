using Domain.Modules.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModuleEntity = Domain.Modules.Entities.ModuleEntity;

namespace Infrastructure.Modules.Configuration;

public sealed class ModuleConfiguration
    : IEntityTypeConfiguration<ModuleEntity>
{
    public void Configure(EntityTypeBuilder<ModuleEntity> builder)
    {
        builder.ToTable("MODULES");

        builder.HasKey(module => module.Id);

        builder.Property(module => module.Id)
            .HasColumnName("MODULE_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid  => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(module => module.Name)
            .HasConversion(
                name  => name.Value,
                value => ModuleName.Create(value))
            .HasColumnName("NAME")
            .HasColumnType("VARCHAR2(50)")
            .HasMaxLength(ModuleName.MaxLength)
            .IsRequired();

        builder.Property(module => module.Description)
            .HasColumnName("DESCRIPTION")
            .HasColumnType("CLOB");

        builder.Property(module => module.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(module => module.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP");

        builder.HasIndex(module => module.Name)
            .IsUnique();
    }
}
