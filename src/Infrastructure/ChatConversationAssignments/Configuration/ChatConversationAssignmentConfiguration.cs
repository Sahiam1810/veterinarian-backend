using Domain.AgentHumans.Entities;
using Domain.ChatConversations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatConversationAssignmentEntity = Domain.ChatConversationAssignments.Entities.ChatConversationAssignment;

namespace Infrastructure.ChatConversationAssignments.Configuration;

public sealed class ChatConversationAssignmentConfiguration
    : IEntityTypeConfiguration<ChatConversationAssignmentEntity>
{
    public void Configure(EntityTypeBuilder<ChatConversationAssignmentEntity> builder)
    {
        builder.ToTable("CHAT_CONVERSATION_ASSIGNMENTS");

        builder.HasKey(assignment => assignment.ChatConversationId);

        builder.Property(assignment => assignment.ChatConversationId)
            .HasColumnName("CHAT_CONVERSATIONS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.HasOne<ChatConversation>()
            .WithOne()
            .HasForeignKey<ChatConversationAssignmentEntity>(assignment => assignment.ChatConversationId)
            .HasConstraintName("FK_CHAT_CONVERSATION_ASSIGNMENTS_CHAT_CONVERSATIONS_CHAT_CONVERSATIONS_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(assignment => assignment.AgentHumanId)
            .HasColumnName("AGENT_HUMAN_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.HasValue ? guid.Value.ToString() : null,
                value => value == null ? null : Guid.Parse(value))
            .IsRequired(false);

        builder.HasIndex(assignment => assignment.AgentHumanId)
            .HasDatabaseName("IX_CHAT_CONVERSATION_ASSIGNMENTS_AGENT_HUMAN_ID");

        builder.HasOne<AgentHuman>()
            .WithMany()
            .HasForeignKey(assignment => assignment.AgentHumanId)
            .HasConstraintName("FK_CHAT_CONVERSATION_ASSIGNMENTS_AGENT_HUMANS_AGENT_HUMAN_ID")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(assignment => assignment.AssignedAt)
            .HasColumnName("ASSIGNED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);

        builder.Property(assignment => assignment.UnassignedAt)
            .HasColumnName("UNASSIGNED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);
    }
}
