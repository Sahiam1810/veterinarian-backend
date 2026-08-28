using Domain.AiModels.Entities;
using Domain.ChatConversations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatConversationAiSettingEntity = Domain.ChatConversationAiSettings.Entities.ChatConversationAiSetting;

namespace Infrastructure.ChatConversationAiSettings.Configuration;

public sealed class ChatConversationAiSettingConfiguration
    : IEntityTypeConfiguration<ChatConversationAiSettingEntity>
{
    public void Configure(EntityTypeBuilder<ChatConversationAiSettingEntity> builder)
    {
        builder.ToTable("CHAT_CONVERSATION_AI_SETTINGS");

        builder.HasKey(setting => setting.Id);

        builder.Property(setting => setting.Id)
            .HasColumnName("CHAT_CONVERSATION_AI_SETTING_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(setting => setting.ConversationId)
            .HasColumnName("CONVERSATION_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(setting => setting.ConversationId)
            .HasDatabaseName("IX_CHAT_CONVERSATION_AI_SETTINGS_CONVERSATION_ID");

        builder.HasOne<ChatConversation>()
            .WithMany()
            .HasForeignKey(setting => setting.ConversationId)
            .HasConstraintName("FK_CHAT_CONVERSATION_AI_SETTINGS_CHAT_CONVERSATIONS_CONVERSATION_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(setting => setting.AiEnabled)
            .HasColumnName("AI_ENABLED")
            .HasConversion<int>()
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(setting => setting.DefaultModelId)
            .HasColumnName("DEFAULT_MODEL_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.HasValue ? guid.Value.ToString() : null,
                value => value == null ? null : Guid.Parse(value))
            .IsRequired(false);

        builder.HasIndex(setting => setting.DefaultModelId)
            .HasDatabaseName("IX_CHAT_CONVERSATION_AI_SETTINGS_DEFAULT_MODEL_ID");

        builder.HasOne<AiModel>()
            .WithMany()
            .HasForeignKey(setting => setting.DefaultModelId)
            .HasConstraintName("FK_CHAT_CONVERSATION_AI_SETTINGS_AI_MODELS_DEFAULT_MODEL_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(setting => setting.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(setting => setting.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);
    }
}
