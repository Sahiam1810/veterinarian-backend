using Domain.ChatAiRuns.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatAiRunErrorEntity = Domain.ChatAiRunErrors.Entities.ChatAiRunError;

namespace Infrastructure.ChatAiRunErrors.Configuration;

public sealed class ChatAiRunErrorConfiguration : IEntityTypeConfiguration<ChatAiRunErrorEntity>
{
    public void Configure(EntityTypeBuilder<ChatAiRunErrorEntity> builder)
    {
        builder.ToTable("CHAT_AI_RUN_ERRORS");

        builder.HasKey(error => error.Id);

        builder.Property(error => error.Id)
            .HasColumnName("CHAT_AI_RUN_ERRORS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(error => error.ChatAiRunId)
            .HasColumnName("CHAT_AI_RUNS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(error => error.ChatAiRunId)
            .HasDatabaseName("IX_CHAT_AI_RUN_ERRORS_CHAT_AI_RUNS_ID");

        builder.HasOne<ChatAiRun>()
            .WithMany()
            .HasForeignKey(error => error.ChatAiRunId)
            .HasConstraintName("FK_CHAT_AI_RUN_ERRORS_CHAT_AI_RUNS_CHAT_AI_RUNS_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(error => error.ErrorMessage)
            .HasColumnName("ERROR_MESSAGE")
            .HasColumnType("CLOB")
            .IsRequired();

        builder.Property(error => error.ErrorCode)
            .HasColumnName("ERROR_CODE")
            .HasColumnType("VARCHAR2(80)")
            .HasMaxLength(ChatAiRunErrorEntity.ErrorCodeMaxLength)
            .IsRequired(false);

        builder.Property(error => error.ProviderErrorId)
            .HasColumnName("PROVIDER_ERROR_ID")
            .HasColumnType("VARCHAR2(120)")
            .HasMaxLength(ChatAiRunErrorEntity.ProviderErrorIdMaxLength)
            .IsRequired(false);

        builder.Property(error => error.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Ignore(error => error.UpdatedAt);
    }
}
