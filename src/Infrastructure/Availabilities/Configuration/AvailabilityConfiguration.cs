using Domain.Availabilities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Availabilities.Configuration;

public sealed class AvailabilityConfiguration : IEntityTypeConfiguration<Availability>
{
    public void Configure(EntityTypeBuilder<Availability> builder)
    {
        builder.ToTable("AVAILABILITIES");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("AVAILABILITY_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.VeterinarianId)
            .HasColumnName("VETERINARIAN_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(x => x.DayOfWeek)
            .HasColumnName("DAY_OF_WEEK")
            .HasColumnType("NUMBER")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.StartTime)
            .HasColumnName("START_TIME")
            .HasColumnType("VARCHAR2(30)")
            .HasMaxLength(30)
            .HasConversion(
                time => time.ToString("HH:mm:ss"),
                value => TimeOnly.Parse(value))
            .IsRequired();

        builder.Property(x => x.EndTime)
            .HasColumnName("END_TIME")
            .HasColumnType("VARCHAR2(30)")
            .HasMaxLength(30)
            .HasConversion(
                time => time.ToString("HH:mm:ss"),
                value => TimeOnly.Parse(value))
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
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP");

        builder.HasOne(x => x.Veterinarian)
            .WithMany()
            .HasForeignKey(x => x.VeterinarianId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.VeterinarianId);
    }
}
