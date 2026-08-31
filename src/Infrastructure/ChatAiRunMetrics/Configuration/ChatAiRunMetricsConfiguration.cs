using Domain.ChatAiRuns.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatAiRunMetricsEntity = Domain.ChatAiRunMetrics.Entities.ChatAiRunMetrics;

namespace Infrastructure.ChatAiRunMetrics.Configuration;

public sealed class ChatAiRunMetricsConfiguration : IEntityTypeConfiguration<ChatAiRunMetricsEntity>
{
    public void Configure(EntityTypeBuilder<ChatAiRunMetricsEntity> builder)
    {
        builder.ToTable("CHAT_AI_RUN_METRICS");

        builder.HasKey(metrics => metrics.Id);

        builder.Property(metrics => metrics.Id)
            .HasColumnName("CHAT_AI_RUN_METRICS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(metrics => metrics.ChatAiRunId)
            .HasColumnName("CHAT_AI_RUNS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(metrics => metrics.ChatAiRunId)
            .IsUnique()
            .HasDatabaseName("UX_CHAT_AI_RUN_METRICS_CHAT_AI_RUNS_ID");

        builder.HasOne<ChatAiRun>()
            .WithMany()
            .HasForeignKey(metrics => metrics.ChatAiRunId)
            .HasConstraintName("FK_CHAT_AI_RUN_METRICS_CHAT_AI_RUNS_CHAT_AI_RUNS_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(metrics => metrics.PromptTokens)
            .HasColumnName("PROMPT_TOKENS")
            .HasColumnType("NUMBER(10)")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(metrics => metrics.CompletionTokens)
            .HasColumnName("COMPLETION_TOKENS")
            .HasColumnType("NUMBER(10)")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(metrics => metrics.TotalTokens)
            .HasColumnName("TOTAL_TOKENS")
            .HasColumnType("NUMBER(10)")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(metrics => metrics.Cost)
            .HasColumnName("COST")
            .HasColumnType("NUMBER(18,6)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(metrics => metrics.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Ignore(metrics => metrics.UpdatedAt);
    }
}
