using Domain.AgentHumans.Entities;
using Domain.ChatConversations.Entities;
using Domain.ChatUserProfiles.Entities;
using Domain.SenderTypes.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatParticipantEntity = Domain.ChatParticipants.Entities.ChatParticipant;

namespace Infrastructure.ChatParticipants.Configuration;

public sealed class ChatParticipantConfiguration : IEntityTypeConfiguration<ChatParticipantEntity>
{
    public void Configure(EntityTypeBuilder<ChatParticipantEntity> builder)
    {
        builder.ToTable("CHAT_PARTICIPANTS");

        builder.HasKey(participant => participant.Id);

        // El provider de Oracle no mapea Guid nativamente a VARCHAR2(36);
        // por defecto intentaría usar RAW(16). Se fuerza la conversión
        // explícita guid → string para almacenarlo como texto en VARCHAR2(36).
        builder.Property(participant => participant.Id)
            .HasColumnName("CHAT_PARTICIPANTS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(participant => participant.ChatConversationId)
            .HasColumnName("CHAT_CONVERSATIONS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(participant => participant.ChatConversationId)
            .HasDatabaseName("IX_CHAT_PARTICIPANTS_CHAT_CONVERSATIONS_ID");

        builder.HasOne<ChatConversation>()
            .WithMany()
            .HasForeignKey(participant => participant.ChatConversationId)
            .HasConstraintName("FK_CHAT_PARTICIPANTS_CHAT_CONVERSATIONS_CHAT_CONVERSATIONS_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(participant => participant.ParticipantTypeId)
            .HasColumnName("PARTICIPANT_TYPE_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(participant => participant.ParticipantTypeId)
            .HasDatabaseName("IX_CHAT_PARTICIPANTS_PARTICIPANT_TYPE_ID");

        builder.HasOne<SenderTypeEntity>()
            .WithMany()
            .HasForeignKey(participant => participant.ParticipantTypeId)
            .HasConstraintName("FK_CHAT_PARTICIPANTS_SENDER_TYPES_PARTICIPANT_TYPE_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(participant => participant.ChatUserProfileId)
            .HasColumnName("CHAT_USER_PROFILE_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.HasValue ? guid.Value.ToString() : null,
                value => value == null ? null : Guid.Parse(value))
            .IsRequired(false);

        builder.HasIndex(participant => participant.ChatUserProfileId)
            .HasDatabaseName("IX_CHAT_PARTICIPANTS_CHAT_USER_PROFILE_ID");

        builder.HasOne<ChatUserProfile>()
            .WithMany()
            .HasForeignKey(participant => participant.ChatUserProfileId)
            .HasConstraintName("FK_CHAT_PARTICIPANTS_CHAT_USER_PROFILES_CHAT_USER_PROFILE_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(participant => participant.AgentHumanId)
            .HasColumnName("AGENT_HUMAN_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.HasValue ? guid.Value.ToString() : null,
                value => value == null ? null : Guid.Parse(value))
            .IsRequired(false);

        builder.HasIndex(participant => participant.AgentHumanId)
            .HasDatabaseName("IX_CHAT_PARTICIPANTS_AGENT_HUMAN_ID");

        builder.HasOne<AgentHuman>()
            .WithMany()
            .HasForeignKey(participant => participant.AgentHumanId)
            .HasConstraintName("FK_CHAT_PARTICIPANTS_AGENT_HUMANS_AGENT_HUMAN_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(participant => participant.AiModelId)
            .HasColumnName("AI_MODEL_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.HasValue ? guid.Value.ToString() : null,
                value => value == null ? null : Guid.Parse(value))
            .IsRequired(false);

        builder.Property(participant => participant.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(participant => participant.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);
    }
}
