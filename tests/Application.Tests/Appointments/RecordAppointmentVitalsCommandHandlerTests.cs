using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class RecordAppointmentVitalsCommandHandlerTests
{
    private static readonly Guid AppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ClientPetId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();
    private readonly RecordAppointmentVitalsCommandHandler sut;

    public RecordAppointmentVitalsCommandHandlerTests()
    {
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
        sut = new RecordAppointmentVitalsCommandHandler(unitOfWork);
    }

    [Fact]
    public async Task RV_T01_Handle_updates_vitals_without_changing_status()
    {
        var appointment = CreateAppointment();
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(appointment);

        var command = new RecordAppointmentVitalsCommand(
            AppointmentId,
            12.5m,
            38.4m,
            90,
            26);

        await sut.Handle(command, CancellationToken.None);

        Assert.Equal(12.5m, appointment.Weight);
        Assert.Equal(38.4m, appointment.Temperature);
        Assert.Equal(90, appointment.HeartRate);
        Assert.Equal(26, appointment.RespiratoryRate);
        Assert.Equal(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), appointment.StatusId);
        await appointmentsRepository.Received(1).UpdateAsync(appointment, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // La validación de "> 0" se prueba en RecordAppointmentVitalsCommandValidatorTests
    // (a nivel de FluentValidation, el punto real donde se rechaza antes de que
    // ValidationBehavior deje llegar el comando hasta este handler).

    [Fact]
    public async Task RV_T03_Handle_throws_when_appointment_not_found()
    {
        appointmentsRepository.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns((Appointment?)null);

        var command = new RecordAppointmentVitalsCommand(
            AppointmentId,
            12.5m,
            38.4m,
            90,
            26);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }

    private static Appointment CreateAppointment()
    {
        var statusId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        var appointment = new Appointment(
            ClientPetId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            statusId,
            Guid.NewGuid(),
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            null);
        return appointment;
    }
}
