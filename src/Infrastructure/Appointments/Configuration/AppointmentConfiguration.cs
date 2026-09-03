using Domain.Appointments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Appointments.Configuration;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("APPOINTMENTS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("APPOINTMENT_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.ClientPetId)
            .HasColumnName("CLIENT_PET_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(x => x.VeterinarianId)
            .HasColumnName("VETERINARIAN_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(x => x.ServiceId)
            .HasColumnName("SERVICE_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(x => x.StatusId)
            .HasColumnName("STATUS_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(x => x.AvailabilityId)
            .HasColumnName("AVAILABILITY_ID")
            .HasColumnType("VARCHAR2(36)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value))
            .IsRequired();

        builder.Property(x => x.ScheduledStart)
            .HasColumnName("SCHEDULED_START")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.ScheduledEnd)
            .HasColumnName("SCHEDULED_END")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasColumnName("NOTES")
            .HasColumnType("VARCHAR2(100)")
            .HasMaxLength(100);

        builder.Property(x => x.RequesterPhoneNumber)
            .HasColumnName("REQUESTER_PHONE_NUMBER")
            .HasColumnType("VARCHAR2(20)")
            .HasMaxLength(20)
            .HasConversion(
                phone => phone == null ? null : phone.Value,
                value => string.IsNullOrWhiteSpace(value)
                    ? null
                    : Domain.Appointments.ValueObjects.RequesterPhoneNumber.Create(value))
            .IsRequired(false);

        builder.Property(x => x.BookingRequestKeyHash)
            .HasColumnName("BOOKING_REQUEST_KEY_HASH")
            .HasColumnType("VARCHAR2(64)")
            .HasMaxLength(Domain.Appointments.ValueObjects.BookingRequestKeyHash.Length)
            .HasConversion(
                hash => hash == null ? null : hash.Value,
                value => string.IsNullOrWhiteSpace(value)
                    ? null
                    : Domain.Appointments.ValueObjects.BookingRequestKeyHash.Create(value))
            .IsRequired(false);

        builder.HasIndex(x => x.BookingRequestKeyHash)
            .IsUnique()
            .HasDatabaseName("UX_APPTS_BOOKING_REQ_HASH");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CREATED_AT")
            .HasColumnType("TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UPDATE_AT")
            .HasColumnType("TIMESTAMP");

        builder.HasOne(x => x.ClientPet)
            .WithMany()
            .HasForeignKey(x => x.ClientPetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Veterinarian)
            .WithMany()
            .HasForeignKey(x => x.VeterinarianId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Service)
            .WithMany()
            .HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Status)
            .WithMany()
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Availability)
            .WithMany()
            .HasForeignKey(x => x.AvailabilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
