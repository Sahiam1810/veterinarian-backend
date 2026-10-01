using Domain.HospitalizationSettings.Entities;
using HospitalizationSettingsEntity = Domain.HospitalizationSettings.Entities.HospitalizationSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.HospitalizationSettings.Configuration;

public sealed class HospitalizationSettingsConfiguration
    : IEntityTypeConfiguration<HospitalizationSettingsEntity>
{
    public void Configure(EntityTypeBuilder<HospitalizationSettingsEntity> builder)
    {
        builder.ToTable("HOSPITALIZATION_SETTINGS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("SETTINGS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(value => value.ToString(), value => Guid.Parse(value))
            .ValueGeneratedNever();

        builder.Property(x => x.DailyRate)
            .HasColumnName("DAILY_RATE")
            .HasColumnType("NUMBER(18,2)")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP");
    }
}
