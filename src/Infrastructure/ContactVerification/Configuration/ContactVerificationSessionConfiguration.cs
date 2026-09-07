using Domain.ContactVerification.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.ContactVerification.Configuration;

public sealed class ContactVerificationSessionConfiguration
    : IEntityTypeConfiguration<ContactVerificationSession>
{
    public void Configure(EntityTypeBuilder<ContactVerificationSession> builder)
    {
        builder.ToTable("CONTACT_VERIFICATION_SESSIONS");
        builder.HasKey(session => session.Id);

        builder.Property(session => session.Id)
            .HasColumnName("ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(value => value.ToString(), value => Guid.Parse(value))
            .ValueGeneratedNever();

        builder.Property(session => session.Purpose)
            .HasColumnName("PURPOSE")
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

        builder.Property(session => session.SubjectUserId)
            .HasColumnName("SUBJECT_USER_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                value => value.HasValue ? value.Value.ToString() : null,
                value => value == null ? null : Guid.Parse(value));

        builder.Property(session => session.OtpHash)
            .HasColumnName("OTP_HASH")
            .HasColumnType("VARCHAR2(64)")
            .HasMaxLength(64);

        builder.Property(session => session.ProofHash)
            .HasColumnName("PROOF_HASH")
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

        builder.Property(session => session.ProofExpiresAt)
            .HasColumnName("PROOF_EXPIRES_AT")
            .HasColumnType("TIMESTAMP");

        builder.Property(session => session.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(session => session.UpdatedAt)
            .HasColumnName("UPDATED_AT")
            .HasColumnType("TIMESTAMP");

        builder.Ignore(session => session.IsAlive);

        builder.HasIndex(session => new { session.Purpose, session.DestinationHash, session.Status })
            .HasDatabaseName("IX_CONTACT_VERIF_ACTIVE");

        builder.HasIndex(session => session.ProofHash)
            .HasDatabaseName("IX_CONTACT_VERIF_PROOF");
    }
}
