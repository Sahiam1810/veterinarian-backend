using Domain.AgentHumans.Entities;
using Domain.ChatEscalations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChatEscalationAssignmentEntity = Domain.ChatEscalationAssignments.Entities.ChatEscalationAssignment;

namespace Infrastructure.ChatEscalationAssignments.Configuration;

// Mapeo EF Core de CHAT_ESCALATION_ASSIGNMENTS según el DDL Oracle.
public sealed class ChatEscalationAssignmentConfiguration
    : IEntityTypeConfiguration<ChatEscalationAssignmentEntity>
{
    public void Configure(EntityTypeBuilder<ChatEscalationAssignmentEntity> builder)
    {
        builder.ToTable("CHAT_ESCALATION_ASSIGNMENTS");

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Id)
            .HasColumnName("CHAT_ESCALATION_ASSIGNMENTS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(assignment => assignment.AgentHumanId)
            .HasColumnName("AGENT_HUMAN_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(assignment => assignment.AgentHumanId)
            .HasDatabaseName("IX_CHAT_ESC_ASG_AGT");

        builder.HasOne<AgentHuman>()
            .WithMany()
            .HasForeignKey(assignment => assignment.AgentHumanId)
            .HasConstraintName("FK_CHAT_ESC_ASG_AGT")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(assignment => assignment.ChatEscalationId)
            .HasColumnName("CHAT_ESCALATIONS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(assignment => assignment.ChatEscalationId)
            .HasDatabaseName("IX_CHAT_ESC_ASG_ESC");

        builder.HasOne<ChatEscalation>()
            .WithMany()
            .HasForeignKey(assignment => assignment.ChatEscalationId)
            .HasConstraintName("FK_CHAT_ESC_ASG_ESC")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(assignment => assignment.AssignedAt)
            .HasColumnName("ASSIGNED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);
    }
}
