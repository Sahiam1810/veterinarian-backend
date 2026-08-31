using Domain.AiModels.Entities;
using Domain.AiRunStatuses.Entities;
using Domain.ChatConversations.Entities;
using Domain.ChatMessages.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatAiRunEntity = Domain.ChatAiRuns.Entities.ChatAiRun;

namespace Infrastructure.ChatAiRuns.Configuration;

public sealed class ChatAiRunConfiguration : IEntityTypeConfiguration<ChatAiRunEntity>
{
    public void Configure(EntityTypeBuilder<ChatAiRunEntity> builder)
    {
        builder.ToTable("CHAT_AI_RUNS");

        builder.HasKey(run => run.Id);

        builder.Property(run => run.Id)
            .HasColumnName("CHAT_AI_RUNS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(run => run.ChatConversationId)
            .HasColumnName("CHAT_CONVERSATIONS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(run => run.ChatConversationId)
            .HasDatabaseName("IX_CHAT_AI_RUNS_CHAT_CONVERSATIONS_ID");

        builder.HasIndex(run => new { run.ChatConversationId, run.CreatedAt })
            .HasDatabaseName("IX_CHAT_AI_RUNS_CHAT_CONVERSATIONS_ID_CREATED_AT");

        builder.HasOne<ChatConversation>()
            .WithMany()
            .HasForeignKey(run => run.ChatConversationId)
            .HasConstraintName("FK_CHAT_AI_RUNS_CHAT_CONVERSATIONS_CHAT_CONVERSATIONS_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(run => run.ChatMessageId)
            .HasColumnName("CHAT_MESSAGES_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(run => run.ChatMessageId)
            .HasDatabaseName("IX_CHAT_AI_RUNS_CHAT_MESSAGES_ID");

        builder.HasOne<ChatMessage>()
            .WithMany()
            .HasForeignKey(run => run.ChatMessageId)
            .HasConstraintName("FK_CHAT_AI_RUNS_CHAT_MESSAGES_CHAT_MESSAGES_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(run => run.AiModelId)
            .HasColumnName("AI_MODEL_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(run => run.AiModelId)
            .HasDatabaseName("IX_CHAT_AI_RUNS_AI_MODELS_ID");

        builder.HasOne<AiModel>()
            .WithMany()
            .HasForeignKey(run => run.AiModelId)
            .HasConstraintName("FK_CHAT_AI_RUNS_AI_MODELS_AI_MODEL_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(run => run.AiRunStatusId)
            .HasColumnName("AI_RUNS_STATUSES_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(run => run.AiRunStatusId)
            .HasDatabaseName("IX_CHAT_AI_RUNS_AI_RUN_STATUSES_ID");

        builder.HasOne<AiRunStatusEntity>()
            .WithMany()
            .HasForeignKey(run => run.AiRunStatusId)
            .HasConstraintName("FK_CHAT_AI_RUNS_AI_RUNS_STATUSES_AI_RUNS_STATUSES_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(run => run.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(run => run.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();
    }
}
