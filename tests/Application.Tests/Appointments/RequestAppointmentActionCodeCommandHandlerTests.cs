using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Verification.Abstractions;
using Domain.Appointments.Entities;
using Domain.Verification.Enums;
using NSubstitute;
using Xunit;

namespace Application.Tests.Appointments;

public sealed class RequestAppointmentActionCodeCommandHandlerTests
{
    private static readonly Guid AppointmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 15, 0, 0, TimeSpan.Zero);
    private const string Hash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task Handle_rejects_when_phone_does_not_match_requester()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var appointments = Substitute.For<IAppointmentRepository>();
        unitOfWork.AppointmentsRepository.Returns(appointments);
        appointments.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(CreateAppointment("3001112233"));

        var sut = new RequestAppointmentActionCodeCommandHandler(
            unitOfWork,
            Substitute.For<IAppointmentActionVerificationSessionRepository>(),
            Substitute.For<IOtpProtector>(),
            Substitute.For<IVerificationCodeDispatcher>(),
            Settings(),
            new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sut.Handle(
                new RequestAppointmentActionCodeCommand(
                    AppointmentId,
                    "3009998877",
                    AppointmentVerificationAction.Cancel),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_sends_sms_and_creates_session_when_phone_matches()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var appointments = Substitute.For<IAppointmentRepository>();
        unitOfWork.AppointmentsRepository.Returns(appointments);
        appointments.GetByIdAsync(AppointmentId, Arg.Any<CancellationToken>())
            .Returns(CreateAppointment("3001112233"));

        var sessions = Substitute.For<IAppointmentActionVerificationSessionRepository>();
        sessions.GetActiveByAppointmentAndActionAsync(
                AppointmentId,
                AppointmentVerificationAction.Cancel,
                Arg.Any<CancellationToken>())
            .Returns((Domain.Verification.Entities.AppointmentActionVerificationSession?)null);

        var otp = Substitute.For<IOtpProtector>();
        otp.Create().Returns(new GeneratedOtp("654321", Hash));
        otp.HashPhone("3001112233").Returns(Hash);

        var dispatcher = Substitute.For<IVerificationCodeDispatcher>();
        var sut = new RequestAppointmentActionCodeCommandHandler(
            unitOfWork,
            sessions,
            otp,
            dispatcher,
            Settings(),
            new FixedTimeProvider(Now));

        var sessionId = await sut.Handle(
            new RequestAppointmentActionCodeCommand(
                AppointmentId,
                "3001112233",
                AppointmentVerificationAction.Cancel),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, sessionId);
        await dispatcher.Received(1).SendAsync(
            VerificationDeliveryChannel.Sms,
            "3001112233",
            "654321",
            Now.AddMinutes(5),
            Arg.Any<CancellationToken>());
        await sessions.Received(1).AddAsync(
            Arg.Any<Domain.Verification.Entities.AppointmentActionVerificationSession>(),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Appointment CreateAppointment(string phone) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.UtcDateTime,
            Now.UtcDateTime.AddHours(1),
            null,
            phone);

    private static IAppointmentVerificationSettings Settings()
    {
        var settings = Substitute.For<IAppointmentVerificationSettings>();
        settings.OtpLifetime.Returns(TimeSpan.FromMinutes(5));
        settings.OtpMaximumAttempts.Returns(5);
        settings.OtpResendInterval.Returns(TimeSpan.FromSeconds(60));
        return settings;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
