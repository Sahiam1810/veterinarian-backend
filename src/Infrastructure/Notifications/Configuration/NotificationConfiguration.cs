using Domain.Notifications.Entities;
using Domain.Notifications.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Notifications.Configuration;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("NOTIFICATIONS", table =>
        {
            // Exactamente uno: usuario del personal o cliente.
            table.HasCheckConstraint(
                "CK_NOTIFICATIONS_ONE_RECIPIENT",
                "(\"USER_ID\" IS NOT NULL AND \"CLIENT_ID\" IS NULL) OR (\"USER_ID\" IS NULL AND \"CLIENT_ID\" IS NOT NULL)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("NOTIFICATION_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasColumnName("USER_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired(false);

        builder.Property(x => x.ClientId)
            .HasColumnName("CLIENT_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired(false);

        builder.Property(x => x.AppointmentId)
            .HasColumnName("APPOINTMENT_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(x => x.Message)
            .HasConversion(
                message => message.Value,
                value => NotificationMessage.Create(value))
            .HasColumnName("MESSAGE")
            .HasColumnType("VARCHAR2(1000)")
            .HasMaxLength(NotificationMessage.MaxLength)
            .IsRequired();

        builder.Property(x => x.SentAt)
            .HasColumnName("SENT_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion(
                status => status.Value,
                value => NotificationStatus.Create(value))
            .HasColumnName("STATUS")
            .HasColumnType("VARCHAR2(20)")
            .HasMaxLength(NotificationStatus.MaxLength)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion(
                type => type.Value,
                value => NotificationType.Create(value))
            .HasColumnName("TYPE")
            .HasColumnType("VARCHAR2(20)")
            .HasMaxLength(NotificationType.MaxLength)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATE_AT")
            .HasColumnType("TIMESTAMP");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Client)
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
