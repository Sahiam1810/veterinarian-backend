using Domain.Appointments.Entities;
using Domain.Appointments.ValueObjects;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class AppointmentBookingIdempotencyTests
{
    [Fact]
    public void SelfServiceAppointment_keeps_booking_hash()
    {
        var hash = new string('A', BookingRequestKeyHash.Length);

        var appointment = CreateAppointment(hash);

        Assert.Equal(hash, appointment.BookingRequestKeyHash?.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG")]
    public void Booking_hash_rejects_invalid_values(string value)
    {
        Assert.Throws<ArgumentException>(() => BookingRequestKeyHash.Create(value));
    }

    [Fact]
    public void AdministrativeAppointment_keeps_booking_hash_empty()
    {
        var appointment = CreateAppointment();

        Assert.Null(appointment.BookingRequestKeyHash);
    }

    private static Appointment CreateAppointment(string? bookingRequestKeyHash = null)
    {
        return new Appointment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddMinutes(30),
            null,
            "3001234567",
            bookingRequestKeyHash);
    }
}
