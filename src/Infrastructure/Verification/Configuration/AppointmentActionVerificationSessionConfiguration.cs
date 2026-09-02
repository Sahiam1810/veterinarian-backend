using Domain.Verification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Verification.Configuration;

public sealed class AppointmentActionVerificationSessionConfiguration
    : IEntityTypeConfiguration<AppointmentActionVerificationSession>
{
    public void Configure(EntityTypeBuilder<AppointmentActionVerificationSession> builder)
    {
        builder.ToTable("APPOINTMENT_ACTION_VERIFICATION_SESSIONS");
        builder.HasKey(session => session.Id);

        builder.Property(session => session.Id)
            .HasColumnName("ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(value => value.ToString(), value => Guid.Parse(value))
            .ValueGeneratedNever();

        builder.Property(session => session.AppointmentId)
            .HasColumnName("APPOINTMENT_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(value => value.ToString(), value => Guid.Parse(value))
            .IsRequired();

        builder.Property(session => session.Action)
            .HasColumnName("ACTION")
            .HasColumnType("VARCHAR2(20)")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(session => session.Channel)
            .HasColumnName("CHANNEL")
            .HasColumnType("VARCHAR2(20)")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(session => session.DestinationHash)
            .HasColumnName("DESTINATION_HASH")
            .HasColumnType("VARCHAR2(64)")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(session => session.OtpHash)
            .HasColumnName("OTP_HASH")
            .HasColumnType("VARCHAR2(64)")
            .HasMaxLength(64);

        builder.Property(session => session.Status)
            .HasColumnName("STATUS")
            .HasColumnType("VARCHAR2(20)")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(session => session.Attempts)
            .HasColumnName("ATTEMPTS")
            .HasColumnType("NUMBER(10)")
            .IsRequired();

        builder.Property(session => session.ExpiresAt)
            .HasColumnName("EXPIRES_AT")
            .HasColumnType("TIMESTAMP");

        builder.Property(session => session.ActionPayload)
            .HasColumnName("ACTION_PAYLOAD")
            .HasColumnType("VARCHAR2(1000)")
            .HasMaxLength(1000);

        builder.Property(session => session.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(session => session.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP");

        builder.HasIndex(session => new { session.AppointmentId, session.Action, session.Status })
            .HasDatabaseName("IX_APPT_ACTION_VERIF_ACTIVE");

        builder.HasIndex(session => session.DestinationHash)
            .HasDatabaseName("IX_APPT_ACTION_VERIF_DEST");
    }
}
