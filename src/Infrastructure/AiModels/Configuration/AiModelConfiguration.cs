using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiModelEntity = Domain.AiModels.Entities.AiModel;
using ProviderEntity = Domain.ProviderModelsAi.Entities.ProviderModelAi;

namespace Infrastructure.AiModels.Configuration;

public sealed class AiModelConfiguration : IEntityTypeConfiguration<AiModelEntity>
{
    public void Configure(EntityTypeBuilder<AiModelEntity> builder)
    {
        builder.ToTable("AI_MODELS");

        builder.HasKey(model => model.Id);

        // El provider de Oracle no mapea Guid nativamente a VARCHAR2(36);
        // por defecto intentaría usar RAW(16). Se fuerza la conversión
        // explícita guid → string para almacenarlo como texto en VARCHAR2(36).
        builder.Property(model => model.Id)
            .HasColumnName("AI_MODEL_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(model => model.ProviderModelAiId)
            .HasColumnName("PROVIDER_MODEL_AI_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasOne<ProviderEntity>()
            .WithMany()
            .HasForeignKey(model => model.ProviderModelAiId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(model => model.NameModel)
            .HasColumnName("NAME_MODEL")
            .HasColumnType("VARCHAR2(150)")
            .IsRequired();

        builder.Property(model => model.ModelKey)
            .HasColumnName("MODEL_KEY")
            .HasColumnType("VARCHAR2(150)")
            .IsRequired();

        builder.Property(model => model.InputTokenPrice)
            .HasColumnName("INPUT_TOKEN_PRICE")
            .HasColumnType("NUMBER(18,6)")
            .IsRequired();

        builder.Property(model => model.OutputTokenPrice)
            .HasColumnName("OUTPUT_TOKEN_PRICE")
            .HasColumnType("NUMBER(18,6)")
            .IsRequired();

        builder.Property(model => model.MaxTokens)
            .HasColumnName("MAX_TOKENS")
            .HasColumnType("NUMBER(10)")
            .IsRequired();

        builder.Property(model => model.ContextWindow)
            .HasColumnName("CONTEXT_WINDOW")
            .HasColumnType("NUMBER(10)")
            .IsRequired();

        builder.Property(model => model.IsActive)
            .HasColumnName("IS_ACTIVE")
            .HasConversion<int>()
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(model => model.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(model => model.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);
    }
}
