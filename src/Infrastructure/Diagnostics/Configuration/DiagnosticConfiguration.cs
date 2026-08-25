using Domain.Diagnostics.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Diagnostics.Configuration;

public class DiagnosticConfiguration : IEntityTypeConfiguration<Diagnostic>
{
    public void Configure(EntityTypeBuilder<Diagnostic> builder)
    {
        builder.ToTable("DIAGNOSTICS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                str => Guid.Parse(str))
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.Code)
            .HasColumnName("CODE")
            .HasMaxLength(15)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.Name)
            .HasColumnName("NAME")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("DESCRIPTION")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .HasColumnName("IS_ACTIVE")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .IsRequired(false);
    }
}