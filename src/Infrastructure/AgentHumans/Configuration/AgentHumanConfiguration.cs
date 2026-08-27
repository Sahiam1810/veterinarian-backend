using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AgentHumanEntity = Domain.AgentHumans.Entities.AgentHuman;
using UserEntity = Domain.Users.Entities.Users;

namespace Infrastructure.AgentHumans.Configuration;

public sealed class AgentHumanConfiguration : IEntityTypeConfiguration<AgentHumanEntity>
{
    public void Configure(EntityTypeBuilder<AgentHumanEntity> builder)
    {
        builder.ToTable("AGENT_HUMANS");

        builder.HasKey(agent => agent.Id);

        // El provider de Oracle no mapea Guid nativamente a VARCHAR2(36);
        // por defecto intentaría usar RAW(16). Se fuerza la conversión
        // explícita guid → string para almacenarlo como texto en VARCHAR2(36).
        builder.Property(agent => agent.Id)
            .HasColumnName("AGENT_HUMAN_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(agent => agent.UserId)
            .HasColumnName("USER_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.HasIndex(agent => agent.UserId);

        builder.HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(agent => agent.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(agent => agent.IsActive)
            .HasColumnName("IS_ACTIVE")
            .HasConversion<int>()
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(agent => agent.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(agent => agent.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired(false);
    }
}
