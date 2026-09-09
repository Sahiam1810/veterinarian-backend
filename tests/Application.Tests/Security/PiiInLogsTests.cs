using Application.Appointments.Abstraction;
using Application.Appointments.UseCases;
using Application.Common.Abstractions;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.UseCases;
using Application.Verification.Abstractions;
using Domain.Appointments.Entities;
using Domain.ContactVerification.Entities;
using Domain.ContactVerification.Enums;
using Domain.Verification.Entities;
using Domain.Verification.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Application.Tests.Security;

// Etapa 6.2: logs de dueño sin PII (teléfono, correo, OTP, proof, cédula).
public sealed class PiiInLogsTests
{
    private const string Phone = "3001234567";
    private const string Email = "owner@huellitas.test";
    private const string OtpCode = "654321";
    private const string DestinationHash =
        "ABABABABABABABABABABABABABABABABABABABABABABABABABABABABABABABAB";
    private const string OtpHash =
        "EFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEFEF";

    private static readonly DateTimeOffset Now = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ContactEmail_request_logs_do_not_contain_email_or_otp()
    {
        var logger = new RecordingLogger<ContactEmailVerificationRequestHandler>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var sessions = Substitute.For<IContactVerificationSessionRepository>();
        var otpProtector = Substitute.For<IOtpProtector>();
        var dispatcher = Substitute.For<IVerificationCodeDispatcher>();
        var settings = Substitute.For<IContactVerificationSettings>();

        settings.OtpLifetime.Returns(TimeSpan.FromMinutes(10));
        settings.OtpResendInterval.Returns(TimeSpan.FromMinutes(1));
        settings.OtpMaximumAttempts.Returns(5);
        otpProtector.Create().Returns(new GeneratedOtp(OtpCode, OtpHash));
        otpProtector.HashEmail(Arg.Any<string>()).Returns(DestinationHash);
        sessions.GetActiveByPurposeAndDestinationAsync(
                Arg.Any<ContactVerificationPurpose>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns((ContactVerificationSession?)null);

        var sut = new ContactEmailVerificationRequestHandler(
            unitOfWork,
            sessions,
            otpProtector,
            dispatcher,
            settings,
            new FixedTimeProvider(Now),
            logger);

        await sut.RequestAsync(
            new RequestContactEmailVerification(Email, ContactVerificationPurpose.Register),
            CancellationToken.None);

        AssertLoggedMessagesContain(logger, "SessionId=", "Purpose=");
        AssertNoPii(logger, Email, OtpCode);
    }

    [Fact]
    public async Task AppointmentOtp_request_logs_do_not_contain_phone_or_otp()
    {
        var logger = new RecordingLogger<RequestAppointmentActionCodeCommandHandler>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var appointments = Substitute.For<IAppointmentRepository>();
        var sessions = Substitute.For<IAppointmentActionVerificationSessionRepository>();
        var otpProtector = Substitute.For<IOtpProtector>();
        var dispatcher = Substitute.For<IVerificationCodeDispatcher>();
        var settings = Substitute.For<IAppointmentVerificationSettings>();

        unitOfWork.AppointmentsRepository.Returns(appointments);
        settings.OtpLifetime.Returns(TimeSpan.FromMinutes(10));
        settings.OtpResendInterval.Returns(TimeSpan.FromMinutes(1));
        settings.OtpMaximumAttempts.Returns(5);
        otpProtector.Create().Returns(new GeneratedOtp(OtpCode, OtpHash));
        otpProtector.HashPhone(Arg.Any<string>()).Returns(DestinationHash);

        var appointment = new Appointment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddDays(1).UtcDateTime,
            Now.AddDays(1).AddHours(1).UtcDateTime,
            notes: null,
            requesterPhoneNumber: Phone);
        appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        sessions.GetActiveByAppointmentAndActionAsync(
                appointment.Id,
                AppointmentVerificationAction.Cancel,
                Arg.Any<CancellationToken>())
            .Returns((AppointmentActionVerificationSession?)null);

        var sut = new RequestAppointmentActionCodeCommandHandler(
            unitOfWork,
            sessions,
            otpProtector,
            dispatcher,
            settings,
            new FixedTimeProvider(Now),
            logger);

        await sut.Handle(
            new RequestAppointmentActionCodeCommand(
                appointment.Id,
                Phone,
                AppointmentVerificationAction.Cancel),
            CancellationToken.None);

        AssertLoggedMessagesContain(logger, "AppointmentId=", "SessionId=");
        AssertNoPii(logger, Phone, OtpCode);
    }

    [Fact]
    public async Task AppointmentOtp_delivery_failure_logs_error_code_without_phone()
    {
        var logger = new RecordingLogger<RequestAppointmentActionCodeCommandHandler>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var appointments = Substitute.For<IAppointmentRepository>();
        var sessions = Substitute.For<IAppointmentActionVerificationSessionRepository>();
        var otpProtector = Substitute.For<IOtpProtector>();
        var dispatcher = Substitute.For<IVerificationCodeDispatcher>();
        var settings = Substitute.For<IAppointmentVerificationSettings>();

        unitOfWork.AppointmentsRepository.Returns(appointments);
        settings.OtpLifetime.Returns(TimeSpan.FromMinutes(10));
        settings.OtpResendInterval.Returns(TimeSpan.FromMinutes(1));
        settings.OtpMaximumAttempts.Returns(5);
        otpProtector.Create().Returns(new GeneratedOtp(OtpCode, OtpHash));
        otpProtector.HashPhone(Arg.Any<string>()).Returns(DestinationHash);
        dispatcher.SendAsync(
                Arg.Any<VerificationDeliveryChannel>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("sms down"));

        var appointment = new Appointment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddDays(1).UtcDateTime,
            Now.AddDays(1).AddHours(1).UtcDateTime,
            notes: null,
            requesterPhoneNumber: Phone);
        appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        sessions.GetActiveByAppointmentAndActionAsync(
                appointment.Id,
                AppointmentVerificationAction.Cancel,
                Arg.Any<CancellationToken>())
            .Returns((AppointmentActionVerificationSession?)null);

        var sut = new RequestAppointmentActionCodeCommandHandler(
            unitOfWork,
            sessions,
            otpProtector,
            dispatcher,
            settings,
            new FixedTimeProvider(Now),
            logger);

        await Assert.ThrowsAsync<Application.Common.Exceptions.ConflictException>(() =>
            sut.Handle(
                new RequestAppointmentActionCodeCommand(
                    appointment.Id,
                    Phone,
                    AppointmentVerificationAction.Cancel),
                CancellationToken.None));

        AssertLoggedMessagesContain(logger, "ErrorCode=", "AppointmentId=");
        AssertNoPii(logger, Phone, OtpCode);
    }

    [Fact]
    public void Owner_flow_source_log_templates_do_not_use_pii_placeholders()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "Application", "ContactVerification", "UseCases", "ContactEmailVerificationRequestHandler.cs"),
            Path.Combine(root, "src", "Application", "Appointments", "UseCases", "RequestAppointmentActionCodeCommand.cs"),
            Path.Combine(root, "src", "Application", "Owners", "UseCases", "RegisterOwnerCommandHandler.cs"),
            Path.Combine(root, "src", "Application", "Clients", "UseCases", "GetClientLookupQueryHandler.cs"),
            Path.Combine(root, "src", "Application", "Telegram", "Processing", "ProcessTelegramUpdate.cs"),
            Path.Combine(root, "src", "Infrastructure", "Telegram", "Workers", "TelegramUpdateWorker.cs"),
        };

        string[] forbidden =
        [
            "{PhoneNumber}", "{phone}", "{RequesterPhoneNumber}",
            "{Email}", "{email}", "{EmailAddress}",
            "{Otp}", "{otp}", "{OTP}",
            "{IdentificationNumber}", "{Cedula}", "{IdNumber}",
            "{ContactProof}", "{proof}", "{VerificationProof}",
            "{Code}", "{code}"
        ];

        foreach (var file in files)
        {
            Assert.True(File.Exists(file), $"Falta archivo de inventario PII: {file}");
            var text = File.ReadAllText(file);
            foreach (var token in forbidden)
            {
                // {ErrorCode} es permitido; el token {Code}/{code} suelto no.
                if (token is "{Code}" or "{code}" && text.Contains("{ErrorCode}", StringComparison.Ordinal))
                {
                    var withoutErrorCode = text.Replace("{ErrorCode}", string.Empty, StringComparison.Ordinal);
                    Assert.DoesNotContain(token, withoutErrorCode, StringComparison.Ordinal);
                    continue;
                }

                Assert.DoesNotContain(token, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void DbContext_registration_does_not_enable_sensitive_data_logging()
    {
        var root = FindRepoRoot();
        var di = File.ReadAllText(Path.Combine(root, "src", "Infrastructure", "DependencyInjection.cs"));
        Assert.DoesNotContain(".EnableSensitiveDataLogging(", di, StringComparison.Ordinal);
        Assert.Contains("nunca EnableSensitiveDataLogging", di, StringComparison.Ordinal);
    }

    private static void AssertLoggedMessagesContain(RecordingLoggerBase logger, params string[] needles)
    {
        Assert.NotEmpty(logger.Messages);
        var joined = string.Join('\n', logger.Messages);
        foreach (var needle in needles)
        {
            Assert.Contains(needle, joined, StringComparison.Ordinal);
        }
    }

    private static void AssertNoPii(RecordingLoggerBase logger, params string[] secrets)
    {
        foreach (var message in logger.Messages)
        {
            foreach (var secret in secrets)
            {
                Assert.DoesNotContain(secret, message, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Veterinarian.sln"))
                || Directory.Exists(Path.Combine(dir.FullName, "src", "Application")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("No se encontró la raíz del repositorio para escanear logs.");
    }

    private abstract class RecordingLoggerBase
    {
        public List<string> Messages { get; } = [];
    }

    private sealed class RecordingLogger<T> : RecordingLoggerBase, ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
