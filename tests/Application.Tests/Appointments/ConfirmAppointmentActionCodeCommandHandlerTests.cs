using System.Text.Json;
using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.AppointmentStatusHistories.Abstraction;
using Application.Availabilities.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.StatusAppointments.Abstraction;
using Application.Verification.Abstractions;
using Domain.Appointments.Entities;
using Domain.Availabilities.Entities;
using Domain.StatusAppointments.Entities;
using Domain.Verification.Entities;
using Domain.Verification.Enums;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

// P1 corregido: el reagendado por OTP (canal sin JWT para el chatbot) no
// validaba choques de horario -- CreateAppointmentCommandHandler y
// UpdateAppointmentCommandHandler sí lo hacen vía HasOverlappingAppointmentAsync,
// pero RescheduleAppointmentAsync reasignaba la franja directo. Un cliente podía
// reagendar su cita OTP a un horario ya ocupado por otra cita del mismo
// veterinario (o de su otra mascota) sin ningún rechazo.
public sealed class ConfirmAppointmentActionCodeCommandHandlerTests
{
    private const string Phone = "3001234567";
    private const string PhoneHash = "ABABABABABABABABABABABABABABABABABABABABABABABABABABABABABABABAB";
    private const string Code = "123456";
    private const string OtpHash = "CDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCD";

    private static readonly DateTimeOffset Now = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAppointmentActionVerificationSessionRepository sessions =
        Substitute.For<IAppointmentActionVerificationSessionRepository>();
    private readonly IOtpProtector otpProtector = Substitute.For<IOtpProtector>();
    private readonly IAppointmentVerificationSettings settings =
        Substitute.For<IAppointmentVerificationSettings>();
    private readonly IAppointmentRepository appointmentsRepository =
        Substitute.For<IAppointmentRepository>();
    private readonly IStatusAppointmentRepository statusRepository =
        Substitute.For<IStatusAppointmentRepository>();
    private readonly IAvailabilityRepository availabilitiesRepository =
        Substitute.For<IAvailabilityRepository>();
    private readonly IAppointmentStatusHistoryRepository historiesRepository =
        Substitute.For<IAppointmentStatusHistoryRepository>();

    private readonly ConfirmAppointmentActionCodeCommandHandler sut;

    public ConfirmAppointmentActionCodeCommandHandlerTests()
    {
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);
        unitOfWork.StatusAppointmentsRepository.Returns(statusRepository);
        unitOfWork.AvailabilitiesRepository.Returns(availabilitiesRepository);
        unitOfWork.AppointmentStatusHistoriesRepository.Returns(historiesRepository);
        unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        otpProtector.HashPhone(Arg.Any<string>()).Returns(PhoneHash);
        otpProtector.Verify(Code, OtpHash).Returns(true);

        sut = new ConfirmAppointmentActionCodeCommandHandler(
            unitOfWork, sessions, otpProtector, settings, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task Handle_reschedule_throws_conflict_when_the_new_slot_overlaps_another_appointment()
    {
        var appointment = new Appointment(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(),
            Now.AddDays(1).UtcDateTime, Now.AddDays(1).AddHours(1).UtcDateTime,
            notes: null, requesterPhoneNumber: Phone);

        var agendada = new StatusAppointment("AGENDADA", null);
        var newAvailability = new Availability(
            appointment.VeterinarianId, DayOfWeek.Thursday, new TimeOnly(9, 0), new TimeOnly(17, 0));
        var newStart = Now.AddDays(2).UtcDateTime;
        var newEnd = newStart.AddHours(1);

        var payload = JsonSerializer.Serialize(new AppointmentReschedulePayload(
            newAvailability.Id, newStart, newEnd, Notes: null));
        var session = AppointmentActionVerificationSession.Start(
            appointment.Id, AppointmentVerificationAction.Reschedule,
            VerificationDeliveryChannel.Sms, PhoneHash, OtpHash,
            Now.AddMinutes(10).UtcDateTime, Now.UtcDateTime, payload);

        appointmentsRepository.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        sessions.GetActiveByAppointmentAndActionAsync(
                appointment.Id, AppointmentVerificationAction.Reschedule, Arg.Any<CancellationToken>())
            .Returns(session);
        statusRepository.GetByIdAsync(appointment.StatusId, Arg.Any<CancellationToken>()).Returns(agendada);
        availabilitiesRepository.GetByIdAsync(newAvailability.Id, Arg.Any<CancellationToken>())
            .Returns(newAvailability);
        appointmentsRepository.HasOverlappingAppointmentAsync(
                appointment.ClientPetId, appointment.VeterinarianId, newStart, newEnd,
                excludeAppointmentId: appointment.Id, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new ConfirmAppointmentActionCodeCommand(
            appointment.Id, Phone, Code, AppointmentVerificationAction.Reschedule);

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Equal(appointment.ScheduledStart, appointment.ScheduledStart);
        await appointmentsRepository.DidNotReceive().UpdateAsync(
            Arg.Is<Appointment>(a => a.ScheduledStart == newStart), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_reschedule_succeeds_and_completes_the_session_when_the_new_slot_is_free()
    {
        var appointment = new Appointment(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(),
            Now.AddDays(1).UtcDateTime, Now.AddDays(1).AddHours(1).UtcDateTime,
            notes: null, requesterPhoneNumber: Phone);

        var agendada = new StatusAppointment("AGENDADA", null);
        var newAvailability = new Availability(
            appointment.VeterinarianId, DayOfWeek.Thursday, new TimeOnly(9, 0), new TimeOnly(17, 0));
        var newStart = Now.AddDays(2).UtcDateTime;
        var newEnd = newStart.AddHours(1);

        var payload = JsonSerializer.Serialize(new AppointmentReschedulePayload(
            newAvailability.Id, newStart, newEnd, Notes: "reagendada"));
        var session = AppointmentActionVerificationSession.Start(
            appointment.Id, AppointmentVerificationAction.Reschedule,
            VerificationDeliveryChannel.Sms, PhoneHash, OtpHash,
            Now.AddMinutes(10).UtcDateTime, Now.UtcDateTime, payload);

        appointmentsRepository.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        sessions.GetActiveByAppointmentAndActionAsync(
                appointment.Id, AppointmentVerificationAction.Reschedule, Arg.Any<CancellationToken>())
            .Returns(session);
        statusRepository.GetByIdAsync(appointment.StatusId, Arg.Any<CancellationToken>()).Returns(agendada);
        availabilitiesRepository.GetByIdAsync(newAvailability.Id, Arg.Any<CancellationToken>())
            .Returns(newAvailability);
        appointmentsRepository.HasOverlappingAppointmentAsync(
                appointment.ClientPetId, appointment.VeterinarianId, newStart, newEnd,
                excludeAppointmentId: appointment.Id, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new ConfirmAppointmentActionCodeCommand(
            appointment.Id, Phone, Code, AppointmentVerificationAction.Reschedule);

        await sut.Handle(command, CancellationToken.None);

        Assert.Equal(newStart, appointment.ScheduledStart);
        Assert.Equal(newEnd, appointment.ScheduledEnd);
        Assert.Equal(VerificationSessionStatus.Completed, session.Status);
        await appointmentsRepository.Received(1).UpdateAsync(appointment, Arg.Any<CancellationToken>());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
