using Domain.Services.Entities;
using Domain.TypeServices.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Services.Configuration;

public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("SERVICES");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("SERVICE_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.TypeServiceId)
            .HasColumnName("TYPE_SERVICE_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("NAME")
            .HasColumnType("VARCHAR2(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.DurationMinutes)
            .HasColumnName("DURATION_MINUTES")
            .HasColumnType("NUMBER")
            .IsRequired();

        builder.Property(x => x.Price)
            .HasColumnName("PRICE")
            .HasColumnType("NUMBER")
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("IS_ACTIVE")
            .HasColumnType("CHAR(1)")
            .HasConversion(
                b => b ? 'Y' : 'N',
                c => c == 'Y')
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATE_AT")
            .HasColumnType("TIMESTAMP");

        builder.HasOne(x => x.TypeService)
            .WithMany()
            .HasForeignKey(x => x.TypeServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Name)
            .IsUnique();
    }
}
