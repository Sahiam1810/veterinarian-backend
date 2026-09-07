using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Api.Appointments.Controllers;
using Api.Auth.Controllers;
using Api.Common.Security;
using Api.Tests.Support;
using Application.Appointments.Abstraction;
using Application.AppointmentStatusHistories.Abstraction;
using Application.Common.Abstractions;
using Application.ContactVerification.Abstractions;
using Application.StatusAppointments.Abstraction;
using Application.Verification.Abstractions;
using Domain.Appointments.Entities;
using Domain.ContactVerification.Entities;
using Domain.StatusAppointments.Entities;
using Domain.Verification.Entities;
using Domain.Verification.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace Api.Tests.Appointments;

// Serializa esta suite: la factory muta Environment (global al proceso).
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AppointmentActionOtpHttpTestsCollection
{
    public const string Name = "AppointmentActionOtpHttpTests";
}

// Tarea 5.4: OTP de acciones de cita (chatbot) ≠ ContactVerification Email.
// Handler real + fakes; dispatcher espiado; sin Twilio/Gmail/Oracle reales.
[Collection(AppointmentActionOtpHttpTestsCollection.Name)]
public sealed class AppointmentActionOtpHttpTests : IClassFixture<AppointmentActionOtpApiFactory>
{
    public const string RequesterPhoneNumber = "3001234567";
    public const string OtherPhoneNumber = "3009999999";
    public const string ValidOtpCode = "123456";

    private readonly AppointmentActionOtpApiFactory factory;

    public AppointmentActionOtpHttpTests(AppointmentActionOtpApiFactory factory) =>
        this.factory = factory;

    [Fact]
    public void RequestCode_And_ConfirmCode_AreAllowAnonymous_WithOwnOtpRateLimits()
    {
        var request = typeof(MyAppointmentsController).GetMethod(
            nameof(MyAppointmentsController.RequestCode));
        var confirm = typeof(MyAppointmentsController).GetMethod(
            nameof(MyAppointmentsController.ConfirmCode));
        Assert.NotNull(request);
        Assert.NotNull(confirm);

        Assert.NotEmpty(request!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
        Assert.NotEmpty(confirm!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));

        Assert.Equal(
            RateLimitPolicies.AppointmentOtpRequest,
            request.GetCustomAttribute<EnableRateLimitingAttribute>()!.PolicyName);
        Assert.Equal(
            RateLimitPolicies.AppointmentOtpConfirm,
            confirm.GetCustomAttribute<EnableRateLimitingAttribute>()!.PolicyName);
    }

    [Fact]
    public async Task RequestCode_WithoutAuthorization_WithMatchingRequesterPhone_Returns202_AndDispatchesOtpOnce()
    {
        factory.ResetSpies();
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/appointments/mine/{factory.Appointment.Id}/request-code",
            new { PhoneNumber = RequesterPhoneNumber, Action = "Cancel" });

        Assert.Null(client.DefaultRequestHeaders.Authorization);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.NotEqual(Guid.Empty, document.RootElement.GetProperty("sessionId").GetGuid());

        await factory.CodeDispatcher.Received(1).SendAsync(
            VerificationDeliveryChannel.Sms,
            RequesterPhoneNumber,
            ValidOtpCode,
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
        await factory.Sessions.Received(1).AddAsync(
            Arg.Is<AppointmentActionVerificationSession>(s =>
                s.AppointmentId == factory.Appointment.Id
                && s.Action == AppointmentVerificationAction.Cancel
                && s.DestinationHash == AppointmentActionOtpApiFactory.PhoneHash),
            Arg.Any<CancellationToken>());
        await AssertContactVerificationUntouchedAsync();
    }

    [Fact]
    public async Task ConfirmCode_WithoutAuthorization_WithMatchingPhoneAndCode_Returns204()
    {
        factory.ResetSpies();
        factory.SeedActiveCancelSession();
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/appointments/mine/{factory.Appointment.Id}/confirm-code",
            new
            {
                PhoneNumber = RequesterPhoneNumber,
                Code = ValidOtpCode,
                Action = "Cancel"
            });

        Assert.Null(client.DefaultRequestHeaders.Authorization);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(
            VerificationSessionStatus.Completed,
            factory.LastActiveSession!.Status);
        await factory.CodeDispatcher.DidNotReceive().SendAsync(
            Arg.Any<VerificationDeliveryChannel>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
        await AssertContactVerificationUntouchedAsync();
    }

    [Fact]
    public async Task RequestCode_WithMismatchedRequesterPhone_Returns401_AndDoesNotDispatch()
    {
        factory.ResetSpies();
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/appointments/mine/{factory.Appointment.Id}/request-code",
            new { PhoneNumber = OtherPhoneNumber, Action = "Cancel" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(OtherPhoneNumber, body, StringComparison.Ordinal);
        Assert.DoesNotContain(RequesterPhoneNumber, body, StringComparison.Ordinal);
        await factory.CodeDispatcher.DidNotReceive().SendAsync(
            Arg.Any<VerificationDeliveryChannel>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
        await factory.Sessions.DidNotReceive().AddAsync(
            Arg.Any<AppointmentActionVerificationSession>(),
            Arg.Any<CancellationToken>());
        await AssertContactVerificationUntouchedAsync();
    }

    [Fact]
    public async Task ConfirmCode_WithMismatchedRequesterPhone_Returns401_AndDoesNotCompleteAction()
    {
        factory.ResetSpies();
        factory.SeedActiveCancelSession();
        using var client = factory.CreateAnonymousClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/appointments/mine/{factory.Appointment.Id}/confirm-code",
            new
            {
                PhoneNumber = OtherPhoneNumber,
                Code = ValidOtpCode,
                Action = "Cancel"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(OtherPhoneNumber, body, StringComparison.Ordinal);
        Assert.DoesNotContain(RequesterPhoneNumber, body, StringComparison.Ordinal);
        Assert.Equal(
            VerificationSessionStatus.AwaitingOtp,
            factory.LastActiveSession!.Status);
        await factory.AppointmentsRepository.DidNotReceive().UpdateAsync(
            Arg.Any<Appointment>(),
            Arg.Any<CancellationToken>());
        await AssertContactVerificationUntouchedAsync();
    }

    private async Task AssertContactVerificationUntouchedAsync()
    {
        await factory.ContactEmailRequest.DidNotReceive().RequestAsync(
            Arg.Any<RequestContactEmailVerification>(),
            Arg.Any<CancellationToken>());
        await factory.ContactSessions.DidNotReceive().GetByIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await factory.ContactSessions.DidNotReceive().AddAsync(
            Arg.Any<ContactVerificationSession>(),
            Arg.Any<CancellationToken>());
    }
}

public sealed class AppointmentActionOtpApiFactory : WebApplicationFactory<AuthController>
{
    public const string PhoneHash =
        "ABABABABABABABABABABABABABABABABABABABABABABABABABABABABABABABAB";
    public const string OtpHash =
        "CDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCDCD";

    private static readonly DateTimeOffset Now = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();
    private readonly Dictionary<string, string?> originalEnvironment = [];

    public Appointment Appointment { get; }
    public IAppointmentRepository AppointmentsRepository { get; } =
        Substitute.For<IAppointmentRepository>();
    public IAppointmentActionVerificationSessionRepository Sessions { get; } =
        Substitute.For<IAppointmentActionVerificationSessionRepository>();
    public IVerificationCodeDispatcher CodeDispatcher { get; } =
        Substitute.For<IVerificationCodeDispatcher>();
    public IOtpProtector OtpProtector { get; } = Substitute.For<IOtpProtector>();
    public IAppointmentVerificationSettings Settings { get; } =
        Substitute.For<IAppointmentVerificationSettings>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public IStatusAppointmentRepository StatusRepository { get; } =
        Substitute.For<IStatusAppointmentRepository>();
    public IAppointmentStatusHistoryRepository HistoriesRepository { get; } =
        Substitute.For<IAppointmentStatusHistoryRepository>();
    public IRequestContactEmailVerification ContactEmailRequest { get; } =
        Substitute.For<IRequestContactEmailVerification>();
    public IContactVerificationSessionRepository ContactSessions { get; } =
        Substitute.For<IContactVerificationSessionRepository>();

    public AppointmentActionVerificationSession? LastActiveSession { get; private set; }

    public AppointmentActionOtpApiFactory()
    {
        foreach (var setting in CreateEnvironment())
        {
            originalEnvironment[setting.Key] = Environment.GetEnvironmentVariable(setting.Key);
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }

        Appointment = new Appointment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddDays(1).UtcDateTime,
            Now.AddDays(1).AddHours(1).UtcDateTime,
            notes: null,
            requesterPhoneNumber: AppointmentActionOtpHttpTests.RequesterPhoneNumber);

        ConfigureFakes();
    }

    public void ResetSpies()
    {
        CodeDispatcher.ClearReceivedCalls();
        Sessions.ClearReceivedCalls();
        AppointmentsRepository.ClearReceivedCalls();
        ContactEmailRequest.ClearReceivedCalls();
        ContactSessions.ClearReceivedCalls();
        LastActiveSession = null;
        ConfigureFakes();
    }

    public void SeedActiveCancelSession()
    {
        LastActiveSession = AppointmentActionVerificationSession.Start(
            Appointment.Id,
            AppointmentVerificationAction.Cancel,
            VerificationDeliveryChannel.Sms,
            PhoneHash,
            OtpHash,
            Now.AddMinutes(10).UtcDateTime,
            Now.UtcDateTime);

        Sessions.GetActiveByAppointmentAndActionAsync(
                Appointment.Id,
                AppointmentVerificationAction.Cancel,
                Arg.Any<CancellationToken>())
            .Returns(LastActiveSession);
    }

    private void ConfigureFakes()
    {
        Settings.OtpLifetime.Returns(TimeSpan.FromMinutes(10));
        Settings.OtpResendInterval.Returns(TimeSpan.FromMinutes(1));
        Settings.OtpMaximumAttempts.Returns(5);

        OtpProtector.Create().Returns(
            new GeneratedOtp(AppointmentActionOtpHttpTests.ValidOtpCode, OtpHash));
        OtpProtector.HashPhone(AppointmentActionOtpHttpTests.RequesterPhoneNumber)
            .Returns(PhoneHash);
        OtpProtector.Verify(AppointmentActionOtpHttpTests.ValidOtpCode, OtpHash).Returns(true);

        AppointmentsRepository.GetByIdAsync(Appointment.Id, Arg.Any<CancellationToken>())
            .Returns(Appointment);
        UnitOfWork.AppointmentsRepository.Returns(AppointmentsRepository);
        UnitOfWork.StatusAppointmentsRepository.Returns(StatusRepository);
        UnitOfWork.AppointmentStatusHistoriesRepository.Returns(HistoriesRepository);
        UnitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var agendada = new StatusAppointment("AGENDADA", null);
        typeof(StatusAppointment).GetProperty(nameof(StatusAppointment.Id))!
            .SetValue(agendada, Appointment.StatusId);
        var cancelada = new StatusAppointment("CANCELADA", null);
        StatusRepository.GetByIdAsync(Appointment.StatusId, Arg.Any<CancellationToken>())
            .Returns(agendada);
        StatusRepository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([agendada, cancelada]);

        Sessions.GetActiveByAppointmentAndActionAsync(
                Appointment.Id,
                Arg.Any<AppointmentVerificationAction>(),
                Arg.Any<CancellationToken>())
            .Returns((AppointmentActionVerificationSession?)null);

        CodeDispatcher.SendAsync(
                Arg.Any<VerificationDeliveryChannel>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    private static Dictionary<string, string> CreateEnvironment() =>
        new()
        {
            ["ConnectionStrings__DefaultConnection"] =
                "User Id=unused;Password=unused;Data Source=unused",
            ["Agent__Enabled"] = "false",
            ["Cors__AllowedOrigins__0"] = "https://frontend.huellitas.test",
            ["Jwt__Issuer"] = "https://issuer.huellitas.appointment-otp-tests",
            ["Jwt__Audience"] = "huellitas-api-appointment-otp-tests",
            ["Jwt__PrivateKeyPemBase64"] = Keys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = Keys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = "appointment-otp-test-key",
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0",
            // Evita proveedores externos en ValidateOnStart; el envío se fakes via IVerificationCodeDispatcher.
            ["Email__Enabled"] = "false",
            ["Twilio__Enabled"] = "false",
            ["Telegram__Enabled"] = "false",
            ["RateLimiting__GlobalPermitLimit"] = "1000",
            ["RateLimiting__GlobalWindowSeconds"] = "60",
            ["RateLimiting__AppointmentOtpRequestPermitLimit"] = "1000",
            ["RateLimiting__AppointmentOtpRequestWindowSeconds"] = "60",
            ["RateLimiting__AppointmentOtpConfirmPermitLimit"] = "1000",
            ["RateLimiting__AppointmentOtpConfirmWindowSeconds"] = "60"
        };

    public HttpClient CreateAnonymousClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUnitOfWork>();
            services.AddSingleton(UnitOfWork);
            services.RemoveAll<IAppointmentActionVerificationSessionRepository>();
            services.AddSingleton(Sessions);
            services.RemoveAll<IVerificationCodeDispatcher>();
            services.AddSingleton(CodeDispatcher);
            services.RemoveAll<IOtpProtector>();
            services.AddSingleton(OtpProtector);
            services.RemoveAll<IAppointmentVerificationSettings>();
            services.AddSingleton(Settings);
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));

            services.RemoveAll<IRequestContactEmailVerification>();
            services.AddSingleton(ContactEmailRequest);
            services.RemoveAll<IContactVerificationSessionRepository>();
            services.AddSingleton(ContactSessions);
        });
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            base.Dispose(disposing);
        }
        finally
        {
            foreach (var setting in originalEnvironment)
            {
                Environment.SetEnvironmentVariable(setting.Key, setting.Value);
            }
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
