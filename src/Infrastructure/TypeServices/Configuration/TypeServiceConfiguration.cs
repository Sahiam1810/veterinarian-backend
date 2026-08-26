using Domain.TypeServices.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.TypeServices.Configuration;

public sealed class TypeServiceConfiguration : IEntityTypeConfiguration<TypeService>
{
    public void Configure(EntityTypeBuilder<TypeService> builder)
    {
        builder.ToTable("TYPE_SERVICES");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("TYPE_SERVICE_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasColumnName("NAME")
            .HasColumnType("VARCHAR2(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("DESCRIPTION")
            .HasColumnType("VARCHAR2(200)")
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP");

        builder.HasIndex(x => x.Name)
            .IsUnique();
    }
}
