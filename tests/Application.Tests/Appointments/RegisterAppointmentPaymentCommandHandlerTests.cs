using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class RegisterAppointmentPaymentCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAppointmentRepository _appointments = Substitute.For<IAppointmentRepository>();
    private readonly RegisterAppointmentPaymentCommandHandler _handler;

    public RegisterAppointmentPaymentCommandHandlerTests()
    {
        _unitOfWork.AppointmentsRepository.Returns(_appointments);
        _handler = new RegisterAppointmentPaymentCommandHandler(_unitOfWork);
    }

    private static Appointment BuildAppointment() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            null);

    [Fact]
    public async Task Marks_the_appointment_as_paid()
    {
        var appointment = BuildAppointment();
        _appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>())
            .Returns(appointment);

        await _handler.Handle(new RegisterAppointmentPaymentCommand(appointment.Id), CancellationToken.None);

        Assert.True(appointment.IsPaid);
        Assert.NotNull(appointment.PaidAt);
        await _appointments.Received(1).UpdateAsync(appointment, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_when_the_appointment_is_already_paid()
    {
        var appointment = BuildAppointment();
        appointment.RegisterPayment();
        _appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>())
            .Returns(appointment);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(new RegisterAppointmentPaymentCommand(appointment.Id), CancellationToken.None));
        Assert.Equal("La cita ya está pagada.", ex.Message);
    }

    [Fact]
    public async Task Throws_not_found_when_the_appointment_does_not_exist()
    {
        var missingId = Guid.NewGuid();
        _appointments.GetByIdAsync(missingId, Arg.Any<CancellationToken>())
            .Returns((Appointment?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new RegisterAppointmentPaymentCommand(missingId), CancellationToken.None));
    }
}
