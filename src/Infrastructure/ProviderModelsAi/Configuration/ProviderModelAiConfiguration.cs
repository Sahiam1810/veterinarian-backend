using Domain.ProviderModelsAi.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProviderEntity = Domain.ProviderModelsAi.Entities.ProviderModelAi;

namespace Infrastructure.ProviderModelsAi.Configuration;

public sealed class ProviderModelAiConfiguration : IEntityTypeConfiguration<ProviderEntity>
{
    public void Configure(EntityTypeBuilder<ProviderEntity> builder)
    {
        builder.ToTable("PROVIDER_MODELS_AI");

        builder.HasKey(provider => provider.Id);

        // El provider de Oracle no mapea Guid nativamente a VARCHAR2(36);
        // por defecto intentaría usar RAW(16). Se fuerza la conversión
        // explícita guid → string para almacenarlo como texto en VARCHAR2(36).
        builder.Property(provider => provider.Id)
            .HasColumnName("PROVIDER_MODEL_AI_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(provider => provider.NameProviderAi)
            .HasColumnName("NAME_PROVIDER_AI")
            .HasColumnType("VARCHAR2(150)")
            .HasMaxLength(ProviderName.MaxLength)
            .IsRequired();

        builder.Property(provider => provider.BusinessName)
            .HasColumnName("BUSINESS_NAME")
            .HasColumnType("VARCHAR2(200)")
            .IsRequired(false);

        builder.Property(provider => provider.Website)
            .HasColumnName("WEBSITE")
            .HasColumnType("VARCHAR2(500)")
            .IsRequired(false);

        builder.Property(provider => provider.IsActive)
            .HasColumnName("IS_ACTIVE")
            .HasConversion<int>()
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(provider => provider.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(provider => provider.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);
    }
}
