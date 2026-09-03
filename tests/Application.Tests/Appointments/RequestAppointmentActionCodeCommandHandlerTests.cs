using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Verification.Abstractions;
using Domain.Appointments.Entities;
using Domain.Verification.Entities;
using Domain.Verification.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class RequestAppointmentActionCodeCommandHandlerTests
{
    private const string Phone = "3001234567";
    private const string OtherPhone = "3009876543";
    private const string NormalizedPhone = "3001234567";
    private const string PhoneHash = "ABABABABABABABABABABABABABABABABABABABABABABABABABABABABABABABAB";
    private const string GeneratedCode = "654321";
    private const string GeneratedOtpHash = "EFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEF";

    private static readonly DateTimeOffset Now = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAppointmentRepository appointmentsRepository = Substitute.For<IAppointmentRepository>();
    private readonly IAppointmentActionVerificationSessionRepository sessions =
        Substitute.For<IAppointmentActionVerificationSessionRepository>();
    private readonly IOtpProtector otpProtector = Substitute.For<IOtpProtector>();
    private readonly IVerificationCodeDispatcher codeDispatcher = Substitute.For<IVerificationCodeDispatcher>();
    private readonly IAppointmentVerificationSettings settings = Substitute.For<IAppointmentVerificationSettings>();

    private readonly RequestAppointmentActionCodeCommandHandler sut;

    public RequestAppointmentActionCodeCommandHandlerTests()
    {
        unitOfWork.AppointmentsRepository.Returns(appointmentsRepository);

        settings.OtpLifetime.Returns(TimeSpan.FromMinutes(10));
        settings.OtpResendInterval.Returns(TimeSpan.FromMinutes(1));
        settings.OtpMaximumAttempts.Returns(5);

        otpProtector.Create().Returns(new GeneratedOtp(GeneratedCode, GeneratedOtpHash));
        otpProtector.HashPhone(Arg.Any<string>()).Returns(PhoneHash);

        sut = new RequestAppointmentActionCodeCommandHandler(
            unitOfWork, sessions, otpProtector, codeDispatcher, settings, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task Handle_creates_a_session_and_dispatches_the_code_when_there_is_no_active_session()
    {
        var appointment = CreateAppointment(Phone);
        appointmentsRepository.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        sessions.GetActiveByAppointmentAndActionAsync(
                appointment.Id, AppointmentVerificationAction.Cancel, Arg.Any<CancellationToken>())
            .Returns((AppointmentActionVerificationSession?)null);

        var command = new RequestAppointmentActionCodeCommand(
            appointment.Id, Phone, AppointmentVerificationAction.Cancel);

        var sessionId = await sut.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, sessionId);
        await codeDispatcher.Received(1).SendAsync(
            VerificationDeliveryChannel.Sms, NormalizedPhone, GeneratedCode, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await sessions.Received(1).AddAsync(
            Arg.Is<AppointmentActionVerificationSession>(s =>
                s.AppointmentId == appointment.Id
                && s.Action == AppointmentVerificationAction.Cancel
                && s.DestinationHash == PhoneHash
                && s.Status == VerificationSessionStatus.AwaitingOtp),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_Unauthorized_when_the_phone_does_not_match_the_appointment()
    {
        var appointment = CreateAppointment(Phone);
        appointmentsRepository.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);

        var command = new RequestAppointmentActionCodeCommand(
            appointment.Id, OtherPhone, AppointmentVerificationAction.Cancel);

        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.Handle(command, CancellationToken.None));
        await sessions.DidNotReceive().AddAsync(Arg.Any<AppointmentActionVerificationSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_NotFoundException_when_the_appointment_is_missing()
    {
        var appointmentId = Guid.NewGuid();
        appointmentsRepository.GetByIdAsync(appointmentId, Arg.Any<CancellationToken>())
            .Returns((Appointment?)null);

        var command = new RequestAppointmentActionCodeCommand(
            appointmentId, Phone, AppointmentVerificationAction.Cancel);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_throws_BadRequestException_when_reschedule_has_no_payload()
    {
        var appointment = CreateAppointment(Phone);
        appointmentsRepository.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);

        var command = new RequestAppointmentActionCodeCommand(
            appointment.Id, Phone, AppointmentVerificationAction.Reschedule, ActionPayloadJson: null);

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(command, CancellationToken.None));
        await sessions.DidNotReceive().AddAsync(Arg.Any<AppointmentActionVerificationSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_when_reschedule_payload_is_malformed_json()
    {
        var appointment = CreateAppointment(Phone);
        appointmentsRepository.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);

        var command = new RequestAppointmentActionCodeCommand(
            appointment.Id, Phone, AppointmentVerificationAction.Reschedule, ActionPayloadJson: "{not-json");

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_throws_conflict_when_an_active_session_was_just_sent()
    {
        var appointment = CreateAppointment(Phone);
        var activeSession = AppointmentActionVerificationSession.Start(
            appointment.Id, AppointmentVerificationAction.Cancel, VerificationDeliveryChannel.Sms,
            PhoneHash, GeneratedOtpHash,
            expiresAt: Now.AddMinutes(9).UtcDateTime,
            createdAt: Now.UtcDateTime);

        appointmentsRepository.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        sessions.GetActiveByAppointmentAndActionAsync(
                appointment.Id, AppointmentVerificationAction.Cancel, Arg.Any<CancellationToken>())
            .Returns(activeSession);

        var command = new RequestAppointmentActionCodeCommand(
            appointment.Id, Phone, AppointmentVerificationAction.Cancel);

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));
        await sessions.DidNotReceive().AddAsync(Arg.Any<AppointmentActionVerificationSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_cancels_the_stale_active_session_and_issues_a_new_one_after_the_resend_interval()
    {
        var appointment = CreateAppointment(Phone);
        var staleSession = AppointmentActionVerificationSession.Start(
            appointment.Id, AppointmentVerificationAction.Cancel, VerificationDeliveryChannel.Sms,
            PhoneHash, GeneratedOtpHash,
            expiresAt: Now.AddMinutes(30).UtcDateTime,
            createdAt: Now.AddMinutes(-5).UtcDateTime);

        appointmentsRepository.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        sessions.GetActiveByAppointmentAndActionAsync(
                appointment.Id, AppointmentVerificationAction.Cancel, Arg.Any<CancellationToken>())
            .Returns(staleSession);

        var command = new RequestAppointmentActionCodeCommand(
            appointment.Id, Phone, AppointmentVerificationAction.Cancel);

        await sut.Handle(command, CancellationToken.None);

        Assert.Equal(VerificationSessionStatus.Cancelled, staleSession.Status);
        await sessions.Received(1).UpdateAsync(staleSession, Arg.Any<CancellationToken>());
        await sessions.Received(1).AddAsync(Arg.Any<AppointmentActionVerificationSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_conflict_when_the_dispatcher_fails_to_send_the_code()
    {
        var appointment = CreateAppointment(Phone);
        appointmentsRepository.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        sessions.GetActiveByAppointmentAndActionAsync(
                appointment.Id, AppointmentVerificationAction.Cancel, Arg.Any<CancellationToken>())
            .Returns((AppointmentActionVerificationSession?)null);
        codeDispatcher.SendAsync(
                Arg.Any<VerificationDeliveryChannel>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("proveedor caído"));

        var command = new RequestAppointmentActionCodeCommand(
            appointment.Id, Phone, AppointmentVerificationAction.Cancel);

        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(command, CancellationToken.None));
        await sessions.DidNotReceive().AddAsync(Arg.Any<AppointmentActionVerificationSession>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Appointment CreateAppointment(string requesterPhoneNumber) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddDays(1).UtcDateTime,
            Now.AddDays(1).AddHours(1).UtcDateTime,
            notes: null,
            requesterPhoneNumber: requesterPhoneNumber);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
