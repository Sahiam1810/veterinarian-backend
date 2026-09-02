using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.AppointmentStatusHistories.Abstraction;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.StatusAppointments.Abstraction;
using Application.Verification.Abstractions;
using Domain.Appointments.Entities;
using Domain.AppointmentStatusHistories.Entities;
using Domain.StatusAppointments.Entities;
using Domain.Verification.Entities;
using Domain.Verification.Enums;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class ConfirmAppointmentActionCodeCommandHandlerTests
{
    private static readonly Guid AppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ClientPetId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AgendadaId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid CanceladaId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 15, 0, 0, TimeSpan.Zero);
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task Handle_cancels_appointment_with_valid_code()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var appointments = Substitute.For<IAppointmentRepository>();
        var statuses = Substitute.For<IStatusAppointmentRepository>();
        var histories = Substitute.For<IAppointmentStatusHistoryRepository>();
        unitOfWork.AppointmentsRepository.Returns(appointments);
        unitOfWork.StatusAppointmentsRepository.Returns(statuses);
        unitOfWork.AppointmentStatusHistoriesRepository.Returns(histories);
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task>>(0)(
                call.ArgAt<CancellationToken>(1)));

        var appointment = CreateAppointment("3001112233", AgendadaId);
        appointments.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>()).Returns(appointment);

        var agendada = CreateStatus(AgendadaId, "AGENDADA");
        var cancelada = CreateStatus(CanceladaId, "CANCELADA");
        statuses.GetByIdAsync(AgendadaId, Arg.Any<CancellationToken>()).Returns(agendada);
        statuses.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { agendada, cancelada });

        var session = AppointmentActionVerificationSession.Start(
            AppointmentId,
            AppointmentVerificationAction.Cancel,
            VerificationDeliveryChannel.Sms,
            Hash,
            Hash,
            Now.AddMinutes(5).UtcDateTime,
            Now.UtcDateTime);

        var sessions = Substitute.For<IAppointmentActionVerificationSessionRepository>();
        sessions.GetActiveByAppointmentAndActionAsync(
                AppointmentId,
                AppointmentVerificationAction.Cancel,
                Arg.Any<CancellationToken>())
            .Returns(session);

        var otp = Substitute.For<IOtpProtector>();
        otp.HashPhone("3001112233").Returns(Hash);
        otp.Verify("123456", Hash).Returns(true);

        var settings = Substitute.For<IAppointmentVerificationSettings>();
        settings.OtpMaximumAttempts.Returns(5);

        var sut = new ConfirmAppointmentActionCodeCommandHandler(
            unitOfWork,
            sessions,
            otp,
            settings,
            new FixedTimeProvider(Now));

        await sut.Handle(
            new ConfirmAppointmentActionCodeCommand(
                AppointmentId,
                "3001112233",
                "123456",
                AppointmentVerificationAction.Cancel,
                "Cliente canceló"),
            CancellationToken.None);

        Assert.Equal(CanceladaId, appointment.StatusId);
        Assert.Equal(VerificationSessionStatus.Completed, session.Status);
        await histories.Received(1).AddAsync(
            Arg.Any<AppointmentStatusHistory>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_wrong_code_and_increments_attempts()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var appointments = Substitute.For<IAppointmentRepository>();
        unitOfWork.AppointmentsRepository.Returns(appointments);
        appointments.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(CreateAppointment("3001112233", AgendadaId));

        var session = AppointmentActionVerificationSession.Start(
            AppointmentId,
            AppointmentVerificationAction.Cancel,
            VerificationDeliveryChannel.Sms,
            Hash,
            Hash,
            Now.AddMinutes(5).UtcDateTime,
            Now.UtcDateTime);

        var sessions = Substitute.For<IAppointmentActionVerificationSessionRepository>();
        sessions.GetActiveByAppointmentAndActionAsync(
                AppointmentId,
                AppointmentVerificationAction.Cancel,
                Arg.Any<CancellationToken>())
            .Returns(session);

        var otp = Substitute.For<IOtpProtector>();
        otp.HashPhone("3001112233").Returns(Hash);
        otp.Verify("000000", Hash).Returns(false);

        var settings = Substitute.For<IAppointmentVerificationSettings>();
        settings.OtpMaximumAttempts.Returns(5);

        var sut = new ConfirmAppointmentActionCodeCommandHandler(
            unitOfWork,
            sessions,
            otp,
            settings,
            new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.Handle(
                new ConfirmAppointmentActionCodeCommand(
                    AppointmentId,
                    "3001112233",
                    "000000",
                    AppointmentVerificationAction.Cancel),
                CancellationToken.None));

        Assert.Equal(1, session.Attempts);
        Assert.Equal(VerificationSessionStatus.AwaitingOtp, session.Status);
    }

    private static Appointment CreateAppointment(string phone, Guid statusId)
    {
        var appointment = new Appointment(
            ClientPetId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            statusId,
            Guid.NewGuid(),
            Now.UtcDateTime,
            Now.UtcDateTime.AddHours(1),
            null,
            phone);

        typeof(Appointment).GetProperty(nameof(Appointment.Id))!
            .SetValue(appointment, AppointmentId);
        return appointment;
    }

    private static StatusAppointment CreateStatus(Guid id, string name)
    {
        var status = new StatusAppointment(name, "desc");
        typeof(StatusAppointment).GetProperty(nameof(StatusAppointment.Id))!
            .SetValue(status, id);
        return status;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
