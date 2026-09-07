using System.Net;
using System.Net.Http.Json;
using Api.Auth.Controllers;
using Api.Tests.Support;
using Application.Appointments.UseCases;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace Api.Tests.Appointments;

// Tarea 6.1: OTP de cita anónimo + rate limit HTTP 429 (sin Twilio/Gmail).
[Collection(EnvironmentVariablesCollection.Name)]
public sealed class AppointmentOtpRateLimitHttpTests
{
    private static readonly Guid AppointmentId = Guid.Parse("dddddddd-4444-4444-4444-444444444444");

    [Fact]
    public async Task RequestCode_Returns429_WithRateLimitExceeded_WhenPermitLimitIsExceeded()
    {
        using var factory = new AppointmentOtpRequestRateLimitedApiFactory();
        using var client = factory.CreateAnonymousClient();
        var path = $"/api/appointments/mine/{AppointmentId}/request-code";
        var body = new { phoneNumber = "3001112233", action = "Cancel" };

        using var first = await client.PostAsJsonAsync(path, body);
        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);

        factory.Sender.ClearReceivedCalls();

        using var second = await client.PostAsJsonAsync(path, body);
        await RateLimitExceededAssert.EqualsContractAsync(second);

        await factory.Sender.DidNotReceive()
            .Send(Arg.Any<RequestAppointmentActionCodeCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmCode_Returns429_WithRateLimitExceeded_WhenPermitLimitIsExceeded()
    {
        using var factory = new AppointmentOtpConfirmRateLimitedApiFactory();
        using var client = factory.CreateAnonymousClient();
        var path = $"/api/appointments/mine/{AppointmentId}/confirm-code";
        var body = new { phoneNumber = "3001112233", code = "000000", action = "Cancel" };

        using var first = await client.PostAsJsonAsync(path, body);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        factory.Sender.ClearReceivedCalls();

        using var second = await client.PostAsJsonAsync(path, body);
        await RateLimitExceededAssert.EqualsContractAsync(second);

        await factory.Sender.DidNotReceive()
            .Send(Arg.Any<ConfirmAppointmentActionCodeCommand>(), Arg.Any<CancellationToken>());
    }
}

public sealed class AppointmentOtpRequestRateLimitedApiFactory : AppointmentOtpRateLimitedApiFactoryBase
{
    public AppointmentOtpRequestRateLimitedApiFactory()
        : base(BuildEnvironment(
            appointmentOtpRequestPermitLimit: 1,
            appointmentOtpConfirmPermitLimit: 1000))
    {
    }
}

public sealed class AppointmentOtpConfirmRateLimitedApiFactory : AppointmentOtpRateLimitedApiFactoryBase
{
    public AppointmentOtpConfirmRateLimitedApiFactory()
        : base(BuildEnvironment(
            appointmentOtpRequestPermitLimit: 1000,
            appointmentOtpConfirmPermitLimit: 1))
    {
    }
}

public abstract class AppointmentOtpRateLimitedApiFactoryBase : WebApplicationFactory<AuthController>
{
    private static readonly RsaTestKeys Keys = RsaTestKeys.Create();
    private readonly Dictionary<string, string?> originalEnvironment = [];

    public ISender Sender { get; } = Substitute.For<ISender>();

    protected AppointmentOtpRateLimitedApiFactoryBase(Dictionary<string, string> environment)
    {
        foreach (var setting in environment)
        {
            originalEnvironment[setting.Key] = Environment.GetEnvironmentVariable(setting.Key);
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }

        Sender.Send(Arg.Any<RequestAppointmentActionCodeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Guid.Parse("eeeeeeee-5555-5555-5555-555555555555"));
        Sender.Send(Arg.Any<ConfirmAppointmentActionCodeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

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
            services.RemoveAll<ISender>();
            services.AddSingleton(Sender);
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

    protected static Dictionary<string, string> BuildEnvironment(
        int appointmentOtpRequestPermitLimit,
        int appointmentOtpConfirmPermitLimit) =>
        new()
        {
            ["ConnectionStrings__DefaultConnection"] =
                "User Id=unused;Password=unused;Data Source=unused",
            ["Agent__Enabled"] = "false",
            ["Email__Enabled"] = "false",
            ["Twilio__Enabled"] = "false",
            ["Telegram__Enabled"] = "false",
            ["Cors__AllowedOrigins__0"] = "https://frontend.huellitas.test",
            ["Jwt__Issuer"] = "https://issuer.huellitas.appointment-otp-rate-limit-tests",
            ["Jwt__Audience"] = "huellitas-api-appointment-otp-rate-limit-tests",
            ["Jwt__PrivateKeyPemBase64"] = Keys.PrivateKeyPemBase64,
            ["Jwt__PublicKeyPemBase64"] = Keys.PublicKeyPemBase64,
            ["Jwt__KeyId"] = "appointment-otp-rate-limit-test-key",
            ["Jwt__AccessTokenMinutes"] = "15",
            ["Jwt__RefreshTokenDays"] = "7",
            ["Jwt__ClockSkewSeconds"] = "0",
            ["RateLimiting__GlobalPermitLimit"] = "1000",
            ["RateLimiting__GlobalWindowSeconds"] = "60",
            ["RateLimiting__AppointmentOtpRequestPermitLimit"] =
                $"{appointmentOtpRequestPermitLimit}",
            ["RateLimiting__AppointmentOtpRequestWindowSeconds"] = "60",
            ["RateLimiting__AppointmentOtpConfirmPermitLimit"] =
                $"{appointmentOtpConfirmPermitLimit}",
            ["RateLimiting__AppointmentOtpConfirmWindowSeconds"] = "60"
        };
}
